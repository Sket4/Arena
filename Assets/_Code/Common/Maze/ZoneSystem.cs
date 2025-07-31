using Arena.ScriptViz;
using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Arena.Maze
{
    public struct ActivateZoneRequest : IComponentData
    {
        public ZoneId Zone;
    }
    
    [BurstCompile]
    [WithChangeFilter(typeof(ZoneGate))]
    partial struct ZoneGateEventJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Commands;
        public float DeltaTime;

        public void Execute(ScriptVizAspect svizAspect, [ChunkIndexInQuery] int sortIndex, in DynamicBuffer<OnZoneGateChangedEventCommand> events, in ZoneGate zoneGate)
        {
            if (zoneGate.Zone1.Value == zoneGate.Zone2.Value)
            {
                // зоны еще не настроены
                return;
            }

            var code = new ContextDisposeHandle(ref svizAspect, ref Commands, sortIndex, DeltaTime);

            foreach (var command in events)
            {
                if (command.Zone1OutputAddress.IsValid)
                {
                    code.Context.WriteToTemp(zoneGate.Zone1, command.Zone1OutputAddress);
                }
                if (command.Zone2OutputAddress.IsValid)
                {
                    code.Context.WriteToTemp(zoneGate.Zone2, command.Zone2OutputAddress);
                }
                code.Execute(command.CommandAddress);
            }
        }
    }
    
    [BurstCompile]
    [WithChangeFilter(typeof(ZoneId))]
    partial struct ZoneIdChangedEventJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Commands;
        public float DeltaTime;

        public void Execute(ScriptVizAspect svizAspect, [ChunkIndexInQuery] int sortIndex, in DynamicBuffer<OnZoneChangedEventCommand> events, in ZoneId zone)
        {
            var code = new ContextDisposeHandle(ref svizAspect, ref Commands, sortIndex, DeltaTime);

            foreach (var command in events)
            {
                if (command.ZoneIDOutputAddress.IsValid)
                {
                    code.Context.WriteToTemp(zone.Value, command.ZoneIDOutputAddress);
                }
                UnityEngine.Debug.Log($"{svizAspect.Self.Index} zone id changed to {zone.Value}");
                code.Execute(command.CommandAddress);
            }
        }
    }
    
    [BurstCompile]
    partial struct ZoneActivatedEventJob : IJobEntity
    {
        [ReadOnly] public NativeArray<ActivateZoneRequest> Requests;
        
        public EntityCommandBuffer.ParallelWriter Commands;
        public float DeltaTime;

        public void Execute(ScriptVizAspect svizAspect, [ChunkIndexInQuery] int sortIndex, in DynamicBuffer<OnZoneActivatedEventCommand> events)
        {
            var code = new ContextDisposeHandle(ref svizAspect, ref Commands, sortIndex, DeltaTime);
            
            foreach (var request in Requests)
            {
                foreach (var command in events)
                {
                    if (command.ZoneIDOutputAddress.IsValid)
                    {
                        code.Context.WriteToTemp(request.Zone.Value, command.ZoneIDOutputAddress);
                    }
                    code.Execute(command.CommandAddress);
                }
            }
        }
    }
    
    [RequireMatchingQueriesForUpdate]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    [UpdateBefore(typeof(AnimationCommandBufferSystem))]
    [DisableAutoCreation]
    public partial class ZoneSystem : GameSystemBase
    {
        private ZoneGateEventJob zoneGateEventsJob;
        private ZoneActivatedEventJob zoneActivatedEventJob;
        private ZoneIdChangedEventJob zoneIdChangedEventJob;
        private EntityQuery activateZoneRequestQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            zoneGateEventsJob = new ZoneGateEventJob();
            zoneActivatedEventJob = new ZoneActivatedEventJob();
            zoneIdChangedEventJob = new ZoneIdChangedEventJob();
            activateZoneRequestQuery = GetEntityQuery(ComponentType.ReadOnly<ActivateZoneRequest>());
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            zoneGateEventsJob.DeltaTime = deltaTime;
            zoneGateEventsJob.Commands = commands;

            Dependency = zoneGateEventsJob.Schedule(Dependency);

            zoneIdChangedEventJob.DeltaTime = deltaTime;
            zoneIdChangedEventJob.Commands = commands;

            Dependency = zoneIdChangedEventJob.Schedule(Dependency);

            if (activateZoneRequestQuery.IsEmpty == false)
            {
                var newCommands = CreateCommandBuffer();
                newCommands.DestroyEntity(activateZoneRequestQuery);    
                
                var requests =
                    activateZoneRequestQuery.ToComponentDataListAsync<ActivateZoneRequest>(Allocator.TempJob, Dependency, out var deps);
                Dependency = deps;

                zoneActivatedEventJob.Requests = requests.AsDeferredJobArray();
                zoneActivatedEventJob.DeltaTime = deltaTime;
                zoneActivatedEventJob.Commands = newCommands.AsParallelWriter();
            
                Dependency = zoneActivatedEventJob.Schedule(Dependency);
                Dependency = requests.Dispose(Dependency);
            }
        }
    }
}
