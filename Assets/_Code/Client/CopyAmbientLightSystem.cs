using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;

namespace Arena.Client.Presentation
{
    [UpdateAfter(typeof(LightProbeSystem))]
    [UpdateBefore(typeof(RenderingSystem))]
    [RequireMatchingQueriesForUpdate]
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
        }

        void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            updateJob.AmbientLookup.Update(ref state);
            updateJob.Commands = ecb;
            updateJob.Run();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

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

