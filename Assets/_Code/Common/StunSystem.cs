using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(HitQuerySystem))]
    [UpdateBefore(typeof(DestroyHitSystem))]
    public partial class StunSystem : SystemBase
    {
        TimeSystem timeSystem;
        GameCommandBufferSystem commandBufferSystem;
        Entity stunModificatorEntity;
        EntityQuery speedModificatorQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            timeSystem = World.GetExistingSystemManaged<TimeSystem>();
            commandBufferSystem = World.GetOrCreateSystemManaged<GameCommandBufferSystem>();
            stunModificatorEntity = EntityManager.CreateEntity();

            #if UNITY_EDITOR
            EntityManager.SetName(stunModificatorEntity, "Stun modificator shared entity");
            #endif

            speedModificatorQuery = GetEntityQuery(typeof(HitBufferElement), ComponentType.ReadOnly<StunRequest>());
        }

        protected override void OnUpdate()
        {
            var stunRequestsList = speedModificatorQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator, out JobHandle collectRequestsHandle);
            Dependency = JobHandle.CombineDependencies(Dependency, collectRequestsHandle);
            var commands = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var hitJob = new StunHitJob
            {
                StunRequests = stunRequestsList.AsDeferredJobArray(),
                HitType = GetBufferTypeHandle<HitBufferElement>(),
                RequestType = GetComponentTypeHandle<StunRequest>(),
                Commands = commands,
            };

            Entities
                .WithNone<Stunned>()
                .ForEach((Entity entity, int entityInQueryIndex, in Speed speed) =>
            {
                hitJob.Execute(entity, entityInQueryIndex, in speed);
            }).Schedule();


            var stunUpdateJob = new StunUpdateJob()
            {
                Commands = commands,
                CurrentTime = timeSystem.GameTime,
                StunModificatorEntity = stunModificatorEntity
            };

            Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<SpeedModificator> speedModificators, DynamicBuffer<CharacterAbilityBlocker> blockModificators, ref Stunned stunned) =>
            {
                stunUpdateJob.Execute(entity, entityInQueryIndex, speedModificators, blockModificators, ref stunned);
            }).Schedule();

            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        struct StunHitJob
        {
            [ReadOnly]
            public NativeArray<ArchetypeChunk> StunRequests;
            public BufferTypeHandle<HitBufferElement> HitType;
            public ComponentTypeHandle<StunRequest> RequestType;
            public EntityCommandBuffer.ParallelWriter Commands;

            public void Execute(Entity entity, int index, in Speed speed)
            {
                bool added = false;
                Stunned stunned = default;

                for(int c=0; c<StunRequests.Length; c++)
                {
                    var chunk = StunRequests[c];
                    var reqBuffers = chunk.GetBufferAccessor(ref HitType);
                    var requests = chunk.GetNativeArray(ref RequestType);

                    for(int i=0; i<requests.Length; i++)
                    {
                        var hits = reqBuffers[i];

                        if(ProcessTarget(index, entity, hits, requests[i], out Stunned newStunned))
                        {
                            if(added)
                            {
                                if(newStunned.Duration > stunned.Duration)
                                {
                                    stunned.Duration = newStunned.Duration;
                                }
                            }
                            else
                            {
                                added = true;
                                stunned = newStunned;
                            }
                        }
                    }
                }

                if(added)
                {
                    Commands.AddComponent(index, entity, stunned);
                }
            }

            public bool ProcessTarget(int index, Entity hitTarget, DynamicBuffer<HitBufferElement> hits, StunRequest stun, out Stunned stunned)
            {
                for(int i=0; i<hits.Length; i++)
                {
                    var hit = hits[i];

                    if(hitTarget != hit.Value.Target)
                    {
                        continue;
                    }

                    stunned = new Stunned
                    {
                        PendingStart = true,
                        Duration = stun.Duration,
                    };

                    return true;
                }
                stunned = default;
                return false;
            }
        }

        struct StunUpdateJob
        {
            public Entity StunModificatorEntity;
            public double CurrentTime;
            public EntityCommandBuffer.ParallelWriter Commands;

            public void Execute(Entity entity, int index, DynamicBuffer<SpeedModificator> speedModificators, DynamicBuffer<CharacterAbilityBlocker> blockModificators, ref Stunned stunned)
            {
                if(stunned.PendingStart)
                {
                    stunned.StartTime = CurrentTime;
                    stunned.PendingStart = false;
                    stunned.ModificatorOwner = StunModificatorEntity;

                    var speedModificator = new SpeedModificator
                    {
                        Value = new CharacteristicModificator
                        {
                            Value = 0,
                            Operator = ModificatorOperators.MULTIPLY_ACTUAL
                        },
                        Owner = StunModificatorEntity
                    };
                    speedModificators.Add(speedModificator);

                    var blockModificator = new CharacterAbilityBlocker
                    {
                        Entity = StunModificatorEntity,
                        ForceStopCurrentAbility = true
                    };
                    blockModificators.Add(blockModificator);
                }

                if(CurrentTime - stunned.StartTime >= stunned.Duration || stunned.PendingFinish)
                {
                    Commands.RemoveComponent<Stunned>(index, entity);
                    IOwnedModificatorExtensions.RemoveModificatorsWithOwner(StunModificatorEntity, speedModificators);
                    CharacterAbilityBlocker.RemoveFromBuffer(StunModificatorEntity, blockModificators);
                }
            }
        }
    }
}
