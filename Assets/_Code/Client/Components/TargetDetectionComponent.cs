using TzarGames.GameCore;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct TargetDetection : IComponentData
    {
        public float DetectionRadius;
        [HideInInspector]
        public CollisionFilter CollisionFilter;
    }

    [UseDefaultInspector(true)]
    public class TargetDetectionComponent : ComponentDataBehaviour<TargetDetection>
    {
        [System.Serializable]
        struct TraceSettings
        {
            public PhysicsCategoryTags BelongsTo;
            public PhysicsCategoryTags CollidesWith;
        }

        [SerializeField]
        TraceSettings traceSettings;

        protected override void Bake<K>(ref TargetDetection serializedData, K baker)
        {
            serializedData.CollisionFilter = new CollisionFilter
            {
                BelongsTo = traceSettings.BelongsTo.Value,
                CollidesWith = traceSettings.CollidesWith.Value,
                GroupIndex = 0
            };
        }
    }
}
