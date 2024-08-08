using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;

namespace Arena
{
    public struct SplinePathMovement : IComponentData, IEnableableComponent
    {
        public Entity TargetPathEntity;
        public float3 CurrentTargetPoint;
        public int CurrentSplineIndex;
        public bool IsPointSet;
    }

    [Serializable]
    public struct SplinePathMovementSettings : IComponentData
    {
        public float MaxTraverseDistance;
    }
    
    public class SplinePathMovementComponent : ComponentDataBehaviour<SplinePathMovementSettings>
    {
        protected override SplinePathMovementSettings CreateDefaultValue()
        {
            return new SplinePathMovementSettings
            {
                MaxTraverseDistance = 5
            };
        }

        protected override void Bake<K>(ref SplinePathMovementSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent(new SplinePathMovement());
            baker.SetComponentEnabled<SplinePathMovement>(false);
        }
    }
}
