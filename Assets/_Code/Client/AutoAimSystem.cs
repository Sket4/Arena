using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(AbilitySystem))]
    public partial class AutoAimSystem : GameSystemBase
    {
        private EntityQuery targetsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            targetsQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<Height>(),
                ComponentType.ReadOnly<Group>()
                );
        }
        
        public static float angle(float3 a, float3 b)
        {
            return Vector3.Angle(a, b);
            //var abm = a*math.length(b);
            //var bam = b*math.length(a);
            //return 2 * Mathf.Atan2(math.length(abm-bam), math.length(abm+bam)) * Mathf.Rad2Deg;
        }

        protected override void OnSystemUpdate()
        {
            var otherTargetChunks = CreateArchetypeChunkArray(targetsQuery, Allocator.TempJob);

            var translationType = GetComponentTypeHandle<LocalToWorld>(true);
            var heightType = GetComponentTypeHandle<Height>(true);
            var groupType = GetComponentTypeHandle<Group>(true);
            
            // авто-наведение должно срабатывать только при первом появлении сущности с компонентом AutoAim
            // (это достигается благодаря фильтру WithChangedFilter)
            Entities
                .WithReadOnly(translationType)
                .WithReadOnly(heightType)
                .WithReadOnly(groupType)
                .WithReadOnly(otherTargetChunks)
                .WithChangeFilter<AutoAim>()
                .WithDisposeOnCompletion(otherTargetChunks)
                .ForEach((Entity entity, ref LocalTransform transform, in HitQuery hitQuery, in Instigator instigator, in AutoAim autoAim) =>
            {
                if(SystemAPI.HasComponent<Group>(instigator.Value) == false)
                {
                    Debug.LogError("Instigator has no group");
                    return;
                }

                var instigatorGroup = SystemAPI.GetComponent<Group>(instigator.Value);
                var dir = math.forward(transform.Rotation);
                float minAngle = float.MaxValue;
                float3 targetDir = default;

                foreach (var targetChunk in otherTargetChunks)
                {
                    var translations = targetChunk.GetNativeArray(ref translationType);
                    var heights = targetChunk.GetNativeArray(ref heightType);
                    var groups = targetChunk.GetNativeArray(ref groupType);

                    for (var i = 0; i < translations.Length; i++)
                    {
                        var group = groups[i];

                        if (instigatorGroup.ID == group.ID)
                        {
                            continue;
                        }
                        
                        var targetTranslation = translations[i];
                        var targetHeight = heights[i];

                        var targetGroundPoint = targetTranslation.Position;
                        var targetTopPoint = targetGroundPoint;
                        targetTopPoint.y += targetHeight.Value;

                        float angleToTarget;
                        float3 dirToTarget;

                        if (Math3D.ClosestPointsOnTwoLines(
                            out Vector3 closestPointLine1, 
                            out _, 
                            targetGroundPoint, 
                            Vector3.up,
                            transform.Position,
                            dir
                            ))
                        {
                            closestPointLine1.y =
                                math.clamp(closestPointLine1.y, targetGroundPoint.y, targetTopPoint.y);
                            
                            dirToTarget = (float3)closestPointLine1 - transform.Position;
                            angleToTarget = angle(dir, dirToTarget);
                        }
                        else
                        {
                            angleToTarget = 0;
                            dirToTarget = dir;
                        }

                        if (angleToTarget > minAngle)
                        {
                            continue;
                        }

                        minAngle = angleToTarget;
                        targetDir = dirToTarget;
                    }
                }

                if (minAngle <= autoAim.Angle * 0.5f)
                {
                    targetDir = math.normalizesafe(targetDir, dir);
                    transform.Rotation = quaternion.LookRotation( targetDir, new float3(0,1,0));
                }
                
            }).Schedule();
        }
    }
}
