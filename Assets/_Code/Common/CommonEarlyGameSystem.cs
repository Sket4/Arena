using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PreSimulationSystemGroup))]
    public partial class CommonEarlyGameSystem : GameSystemBase
    {
        struct NavMeshDataCleanup : ICleanupComponentData
        {
            public NavMeshDataInstance NavDataInstance;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in NavMeshDataCleanup data) =>
                {   
                    Debug.Log($"removing navmesh data from entity {entity}");
                    NavMesh.RemoveNavMeshData(data.NavDataInstance);
                    EntityManager.RemoveComponent<NavMeshDataCleanup>(entity);
                    
                }).Run();
        }

        protected override unsafe void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();
            var deltaTime = World.Time.DeltaTime;
            
            // проверяем событие смерти заранее, чтобы WasDeadInCurrentFrame работал и в сетевой версии тоже
            // из-за этого событие будет происходить с задержкой в один кадр в одиночной версии игры
            Entities
                .WithChangeFilter<DeathData>()
                .ForEach((
                    Entity entity, 
                    int entityInQueryIndex, 
                    DynamicBuffer<ScriptViz.DeadEventData> deathEvents,
                    DynamicBuffer<VariableDataByte> variableData,
                    DynamicBuffer<EntityVariableData> entityVars,
                    in LivingState livingState, 
                    in ScriptVizCodeInfo codeDataInfo) =>
                {
                    if (livingState.WasDeadInCurrentFrame() == false)
                    {
                        return;
                    }
                    var codeData = SystemAPI.GetBuffer<CodeDataByte>(codeDataInfo.CodeDataEntity);
                    var constantEntityData = SystemAPI.GetBuffer<ConstantEntityVariableData>(codeDataInfo.CodeDataEntity);
                    var codeAsset = new ScriptVizCodeAsset((byte*)codeData.GetUnsafeReadOnlyPtr());

                    using (var stackMemory = new NativeArray<byte>(codeDataInfo.TempMemorySize, Allocator.Temp))
                    {
                        foreach (var deathEvent in deathEvents)
                        {
                            var context = new Context(
                                entity,
                                codeAsset, 
                                ref variableData, 
                                constantEntityData.AsNativeArray(), 
                                entityVars.AsNativeArray(),
                                stackMemory.GetUnsafePtr(), 
                                ref commands, 
                                entityInQueryIndex, 
                                deltaTime
                                //deathEvent.CommandAddress
                                );
                            
                            Extensions.ExecuteCode(ref context, deathEvent.CommandAddress);    
                        }
                    }

                }).Run();
            
            Entities
                .WithNone<DisableAutoDestroy>()
                .ForEach((Entity entity, int entityInQueryIndex, in PlayerController controller) =>
                {
                    if(SystemAPI.Exists(controller.Value) == false)
                    {
                        Debug.Log($"Destroying character {entity.Index}");
                        commands.DestroyEntity(entityInQueryIndex, entity);
                    }

                }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<NavMeshDataCleanup>()
                .ForEach((Entity entity, in NavMeshManagedData navData) =>
                {
                    Debug.Log($"adding navmesh data from entity {entity}");
                    
                    var instance = NavMesh.AddNavMeshData(navData.Data);
                    commands.AddComponent(0, entity, new NavMeshDataCleanup
                    {
                        NavDataInstance = instance
                    });

                }).Run();
            
            Entities
                .WithoutBurst()
                .WithNone<NavMeshManagedData>()
                .ForEach((Entity entity, in NavMeshDataCleanup data) =>
                {   
                    Debug.Log($"removing navmesh data from entity {entity}");
                    NavMesh.RemoveNavMeshData(data.NavDataInstance);
                    commands.RemoveComponent<NavMeshDataCleanup>(0, entity);
                    
                }).Run();
        }
    }
}
