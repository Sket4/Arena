using Arena.ScriptViz;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Splines;

namespace Arena
{
    public struct ReachedSplinePathDestinationEvent : IComponentData
    {
        public Entity Target;
    }
    
    [UpdateInGroup(typeof(PreSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementAISystem))]
    [UpdateBefore(typeof(PathMovementSystem))]
    public partial class SplinePathMovementSystem : GameSystemBase
    {
        private EntityQuery reachedEventsQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            reachedEventsQuery = GetEntityQuery(ComponentType.ReadOnly<ReachedSplinePathDestinationEvent>());
        }

        protected override void OnSystemUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, ref SplinePathMovement movement, ref PathMovement pathMovement, in SplinePathMovementSettings moveSettings, in LocalTransform transform, in SplinePathFollowTarget followTarget) =>
                {
                    if (followTarget.Value != Entity.Null &&
                        EntityManager.HasComponent<LocalToWorld>(followTarget.Value))
                    {
                        var followTargetPos = EntityManager.GetComponentData<LocalToWorld>(followTarget.Value).Position;
                        var distanceSq = math.distancesq(followTargetPos, transform.Position);

                        if (moveSettings.MaxDistanceToFollowTarget < distanceSq)
                        {
                            movement.IsWaitingFollowTarget = true;
                            pathMovement.RequestStop();
                            return;
                        }
                        else
                        {
                            if (movement.IsWaitingFollowTarget)
                            {
                                if (distanceSq <= moveSettings.ContinueFollowDistance)
                                {
                                    movement.IsWaitingFollowTarget = false;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    }
                    
                    var spline = EntityManager.GetComponentObject<SplineContainerReference>(movement.TargetPathEntity);
                    
                    if (movement.IsPointSet == false)
                    {
                        movement.CurrentSplineIndex = 0;
                        SplineUtility.GetNearestPoint(spline.Value.Splines[movement.CurrentSplineIndex], transform.Position, out _, out float t);
                        SplinePathMovement(t, moveSettings, spline, ref pathMovement, ref movement);
                    }

                    bool isReachedDestination = false;

                    if (movement.CurrentSplineIndex >= spline.Value.Splines.Count)
                    {
                        isReachedDestination = true;
                    }

                    if (isReachedDestination)
                    {
                        Debug.Log($"{entity} reached its spline path destination point");
                        EntityManager.SetComponentEnabled<SplinePathMovement>(entity, false);

                        var evtEntity = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(evtEntity, new ReachedSplinePathDestinationEvent
                        {
                            Target = entity
                        });
                        
                        return;
                    }
                    
                    Debug.DrawLine(transform.Position, movement.CurrentTargetPoint, Color.magenta);
                    
                    SplineUtility.GetNearestPoint(spline.Value.Splines[movement.CurrentSplineIndex], transform.Position, out var nearestPosition, out float currentT);
                    
                    var dist = math.distance(movement.CurrentTargetPoint, nearestPosition);

                    if (dist > moveSettings.MaxTraverseDistance * 0.5f)
                    {
                        SplinePathMovement(currentT, moveSettings, spline, ref pathMovement, ref movement);
                    }
                    else
                    {
                        if (pathMovement.IsMovingOnPath == false)
                        {
                            SplinePathMovement(currentT, moveSettings, spline, ref pathMovement, ref movement);
                        }
                    }
                }).Run();

            if (reachedEventsQuery.IsEmpty == false)
            {
                var deltaTime = SystemAPI.Time.DeltaTime;
                var commands = CreateUniversalCommandBuffer();
                var events = reachedEventsQuery.ToComponentDataArray<ReachedSplinePathDestinationEvent>(Allocator.TempJob);
                
                Entities
                    .WithReadOnly(events)
                    .ForEach((ScriptVizAspect aspect, in DynamicBuffer<ReachedSplineDestinationPointCommand> eventCommands) =>
                {
                    foreach (var evt in events)
                    {
                        if (evt.Target != aspect.Self)
                        {
                            continue;
                        }

                        var handle = new ContextDisposeHandle(ref aspect, ref commands, 0, deltaTime);
                        
                        foreach (var command in eventCommands)
                        {
                            if (command.CommandAddress.IsValid)
                            {
                                handle.Execute(command.CommandAddress);    
                            }  
                        }
                        break;
                    }
                    
                }).Run();
                
                EntityManager.DestroyEntity(reachedEventsQuery);
            }
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
