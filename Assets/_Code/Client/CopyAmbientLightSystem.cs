using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Arena.Client.Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(LightProbeSystem))]
    [UpdateBefore(typeof(RenderingSystem))]
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
            state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();
        }

        void OnUpdate(ref SystemState state)
        {
            var cmdBufferSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            updateJob.AmbientLookup.Update(ref state);
            updateJob.Commands = cmdBufferSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            updateJob.Schedule();
        }
    }

    [BurstCompile]
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

