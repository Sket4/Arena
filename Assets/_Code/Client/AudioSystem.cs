using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TzarGames.GameCore;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class AudioSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var l2wLookup = GetComponentLookup<LocalToWorld>(true);
            
            // spline audio
            Entities
                .WithoutBurst()
                .WithReadOnly(l2wLookup)
                .WithAll<SplineAudio>()
                .ForEach((ref LocalTransform transform, in Target target, in SplineAudio splineAudio) =>
            {
                if (l2wLookup.TryGetComponent(target.Value, out var targetTransform) == false)
                {
                    return;
                }
                
                var splineContainer = EntityManager.GetComponentObject<SplineContainerReference>(splineAudio.SplineReference).Value;
                var nearestDistance = float.MaxValue;
                var nearestPoint = float3.zero;

                foreach (var spline in splineContainer.Splines)
                {
                    var distance = SplineUtility.GetNearestPoint(spline, targetTransform.Position, out var nearest, out var t);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPoint = nearest;
                    }
                }

                Debug.DrawLine(nearestPoint, targetTransform.Position, Color.yellow);
                transform.Position = nearestPoint;


            }).Run();
        }
    }
}
