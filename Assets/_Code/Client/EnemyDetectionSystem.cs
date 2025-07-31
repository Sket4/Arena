using TzarGames.GameCore;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class EnemyDetectionSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            EntityCommandBuffer.ParallelWriter commands = CreateEntityCommandBufferParallel();

            Entities
                .WithReadOnly(collisionWorld)
                .ForEach((Entity entity, int entityInQueryIndex, in EnemyDetectionSettings enemyDetectionSettings, in Group group, in EnemyDetectionData enemyDetectionData, in LocalTransform transform) =>
            {
                var cfilter = CollisionFilter.Default;
                cfilter.CollidesWith = enemyDetectionSettings.TraceLayers;

                var hits = new NativeList<DistanceHit>(32, Allocator.Temp);

                if (collisionWorld.OverlapSphere(transform.Position, enemyDetectionSettings.DetectionRadius, ref hits, cfilter, QueryInteraction.IgnoreTriggers) == false)
                {
                    hits.Dispose();

                    if (enemyDetectionData.HasNearEnemy)
                    {
                        var data = enemyDetectionData;
                        data.HasNearEnemy = false;
                        commands.SetComponent(entityInQueryIndex, entity, data);
                    }
                    return;
                }
                using(hits)
                {
                    foreach (var hit in hits)
                    {
                        if(SystemAPI.HasComponent<LootTag>(hit.Entity))
                        {
                            continue;
                        }

                        if(SystemAPI.HasComponent<LivingState>(hit.Entity) == false)
                        {
                            continue;
                        }
                        var otherLivingState = SystemAPI.GetComponent<LivingState>(hit.Entity);

                        if(otherLivingState.IsDead)
                        {
                            continue;
                        }

                        if (SystemAPI.HasComponent<Group>(hit.Entity) == false)
                        {
                            continue;
                        }
                        var otherGroup = SystemAPI.GetComponent<Group>(hit.Entity);
                        if (otherGroup.ID != group.ID)
                        {
                            if (enemyDetectionData.HasNearEnemy == false)
                            {
                                var data = enemyDetectionData;
                                data.HasNearEnemy = true;
                                commands.SetComponent(entityInQueryIndex, entity, data);
                            }
                            return;
                        }
                    }
                }

                if (enemyDetectionData.HasNearEnemy)
                {
                    var data = enemyDetectionData;
                    data.HasNearEnemy = false;
                    commands.SetComponent(entityInQueryIndex, entity, data);
                }

            }).ScheduleParallel();
        }
    }
}
