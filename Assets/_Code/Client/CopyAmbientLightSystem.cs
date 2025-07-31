using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Arena.Client.Presentation
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct CopyAmbientLightSystem : ISystem
    {
        private UpdateJob updateJob;

        void OnCreate(ref SystemState state)
        {
            updateJob = new UpdateJob
            {
                AmbientLookup = state.GetComponentLookup<AmbientCubePackedColorsData>(true)
            };
            state.RequireForUpdate<CopyAmbientLight>();
            state.RequireForUpdate<GameCommandBufferSystem.Singleton>();
        }

        void OnUpdate(ref SystemState state)
        {
            var cmdBufferSingleton = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
            updateJob.AmbientLookup.Update(ref state);
            updateJob.Commands = cmdBufferSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            updateJob.Schedule();
        }
    }

    [BurstCompile]
    [WithAll(typeof(AmbientCubePackedColorsData))]
    partial struct UpdateJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<AmbientCubePackedColorsData> AmbientLookup;

        public EntityCommandBuffer Commands;
    
        public void Execute(Entity entity, in CopyAmbientLight copyFrom)
        {
            if (AmbientLookup.TryGetComponent(copyFrom.Source, out var source) == false)
            {
                return;
            }
            Commands.SetComponent(entity, source);
        }
    }  
}

