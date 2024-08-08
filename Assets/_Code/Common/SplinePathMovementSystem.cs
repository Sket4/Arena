using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Splines;

namespace Arena
{
    [UpdateInGroup(typeof(PreSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementAISystem))]
    [UpdateBefore(typeof(PathMovementSystem))]
    public partial class SplinePathMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((ref SplinePathMovement movement, ref PathMovement pathMovement, in SplinePathMovementSettings moveSettings, in LocalTransform transform) =>
                {
                    var spline = EntityManager.GetComponentObject<SplineContainerReference>(movement.TargetPathEntity);
                    
                    if (movement.IsPointSet == false)
                    {
                        movement.CurrentSplineIndex = 0;
                        SplineUtility.GetNearestPoint(spline.Value.Splines[movement.CurrentSplineIndex], transform.Position, out _, out float t);
                        SplinePathMovement(t, moveSettings, spline, ref pathMovement, ref movement);
                    }

                    if (movement.CurrentSplineIndex >= spline.Value.Splines.Count)
                    {
                        return;
                    }
                    
                    Debug.DrawLine(transform.Position, movement.CurrentTargetPoint, Color.magenta);
                    
                    SplineUtility.GetNearestPoint(spline.Value.Splines[movement.CurrentSplineIndex], transform.Position, out var nearestPosition, out float currentT);
                    
                    var dist = math.distance(movement.CurrentTargetPoint, nearestPosition);

                    if (dist > moveSettings.MaxTraverseDistance * 0.5f)
                    {
                        SplinePathMovement(currentT, moveSettings, spline, ref pathMovement, ref movement);
                    }

                }).Run();
        }

        private static void SplinePathMovement(float nearestT, SplinePathMovementSettings moveSettings,
            SplineContainerReference spline, ref PathMovement pathMovement, ref SplinePathMovement movement)
        {
            nearestT = math.saturate(nearestT);
            
            var tDist = moveSettings.MaxTraverseDistance / spline.CachedLength;
            var nextT = math.min(1.0f, nearestT + tDist);

            var remainingT = 1.0f - nearestT;
            Spline targetSpline = spline.Value.Splines[movement.CurrentSplineIndex];

            //Debug.Log($"current {nearestT}, next {nearestT}, remaining {remainingT} tdist {tDist}");
            
            if (remainingT < tDist)
            {
                movement.CurrentSplineIndex++;
                
                //Debug.Log($"next spline");

                if (movement.CurrentSplineIndex < spline.Value.Splines.Count)
                {
                    targetSpline = spline.Value.Splines[movement.CurrentSplineIndex];
                    nextT = tDist - remainingT;
                }
            }
            
            movement.CurrentTargetPoint = SplineUtility.EvaluatePosition(targetSpline, nextT);
            movement.IsPointSet = true;
                        
            pathMovement.GoToPosition(movement.CurrentTargetPoint);
        }
    }
}
