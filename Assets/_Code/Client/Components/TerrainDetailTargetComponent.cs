using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;

namespace Arena.Client
{
    [Serializable]
    public struct TerrainDetailTarget : IComponentData
    {
        public float DetailCheckDistanceTreshold;
    }

    [Serializable]
    public struct TerrainDetailTargetLastPosition : IComponentData
    {
        public bool IsInitialized;
        public float2 Value;
    }
    
    public class TerrainDetailTargetComponent : ComponentDataBehaviour<TerrainDetailTarget>
    {
        protected override void Bake<K>(ref TerrainDetailTarget serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent(new TerrainDetailTargetLastPosition
            {
                Value = new float2(float.MinValue, float.MaxValue)
            });
        }
    }
}
