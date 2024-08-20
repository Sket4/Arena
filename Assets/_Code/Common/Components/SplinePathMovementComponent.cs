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
        public bool IsWaitingFollowTarget;
    }

    public struct SplinePathFollowTarget : IComponentData
    {
        public Entity Value;
    }

    [Serializable]
    public struct SplinePathMovementSettings : IComponentData
    {
        public float MaxTraverseDistance;
        public float MaxDistanceToFollowTarget;
        public float ContinueFollowDistance;
    }
    
    public class SplinePathMovementComponent : ComponentDataBehaviour<SplinePathMovementSettings>
    {
        protected override SplinePathMovementSettings CreateDefaultValue()
        {
            return new SplinePathMovementSettings
            {
                MaxTraverseDistance = 5,
                MaxDistanceToFollowTarget = 15,
                ContinueFollowDistance = 5
            };
        }

        protected override void Bake<K>(ref SplinePathMovementSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.MaxDistanceToFollowTarget *= serializedData.MaxDistanceToFollowTarget;
            serializedData.ContinueFollowDistance *= serializedData.ContinueFollowDistance;
            baker.AddComponent(new SplinePathMovement());
            baker.AddComponent(new SplinePathFollowTarget());
            baker.SetComponentEnabled<SplinePathMovement>(false);
        }
    }
}
