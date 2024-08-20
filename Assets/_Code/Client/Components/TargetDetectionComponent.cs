using TzarGames.GameCore;
using Unity.Entities;
using Unity.Physics;
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
        [SerializeField] private LayerMask traceLayers;

        protected override void Bake<K>(ref TargetDetection serializedData, K baker)
        {
            serializedData.CollisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = Utility.LayerMaskToCollidesWithMask(traceLayers.value),
                GroupIndex = 0
            };
        }
    }
}
