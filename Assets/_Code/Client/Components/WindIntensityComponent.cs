using System;
using TzarGames.GameCore;
using TzarGames.Rendering;
using TzarGames.Rendering.Baking;
using Unity.Burst;
using Unity.Entities;

namespace Arena.Client
{
    [Serializable]
    public struct WindIntensity : IComponentData
    {
        public float Value;
    }
    
    public class WindIntensityComponent : ComponentDataBehaviour<WindIntensity>
    {
        protected override WindIntensity CreateDefaultValue()
        {
            return new WindIntensity
            {
                Value = 1
            };
        }
    }
    
    #if UNITY_EDITOR
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(RendererBakingSystem))]
    partial struct WindIntensityBakingSystem : ISystem
    {
        [BurstCompile]
        partial struct WriteDataJob : IJobEntity
        {
            public void Execute(ref RendererInstanceData instanceData, in WindIntensity windIntensity)
            {
                instanceData.CommonValue1 = windIntensity.Value;
            }
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var job = new WriteDataJob();
            job.Run();
        }
    }
    #endif
}
