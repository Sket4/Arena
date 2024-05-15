using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public struct WaterState : IComponentData, IEnableableComponent
    {
        [NonSerialized]
        public float Depth;
    }
    
    public class WaterStateComponent : ComponentDataBehaviour<WaterState>
    {
        protected override void Bake<K>(ref WaterState serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.SetComponentEnabled<WaterState>(false);
        }
    }
}
