using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Arena
{
    public struct WaterState : IComponentData, IEnableableComponent
    {
        [HideInAuthoring]
        public float Depth;
    }

    public struct WaterEnterEvent : IComponentData
    {
        public Entity EnteredEntity;
        public float EnterSpeed;
        public float3 EntityLocation;
        public quaternion EntityRotation;
        public float3 WaterPointLocation;

        public const float EnterEventTreshold = 2;
    }

    public struct WaterExitEvent : IComponentData
    {
        public Entity EnteredEntity;
    }

    [Serializable]
    public struct WaterRippleEffectInstance : IComponentData
    {
        public Entity Instance;
    }
    
    public class WaterStateComponent : ComponentDataBehaviour<WaterState>
    {
        protected override void Bake<K>(ref WaterState serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.SetComponentEnabled<WaterState>(false);

            if (ShouldBeConverted(ConversionTargetOptions.LocalAndClient, baker))
            {
                baker.AddComponent(new WaterRippleEffectInstance());
            }
        }
    }
}
