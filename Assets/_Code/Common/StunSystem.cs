using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(HitQuerySystem))]
    [UpdateBefore(typeof(DestroyHitSystem))]
    [UpdateBefore(typeof(AnimationCommandBufferSystem))]
    public partial class StunSystem : SystemBase
    {
        TimeSystem timeSystem;
        GameCommandBufferSystem commandBufferSystem;
        private BufferLookup<SpeedModificator> speedModsLookup;
        private BufferLookup<CharacterAbilityBlocker> abilityBlockerLookup;
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
            speedModsLookup = GetBufferLookup<SpeedModificator>();
            abilityBlockerLookup = GetBufferLookup<CharacterAbilityBlocker>();
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
                Commands = commands
            };

            Entities
                .WithNone<Stunned>()
                .ForEach((Entity entity, int entityInQueryIndex, in Speed speed) =>
            {
                hitJob.Execute(entity, entityInQueryIndex, in speed);
            }).Schedule();


            speedModsLookup.Update(this);
            abilityBlockerLookup.Update(this);
            
            var stunUpdateJob = new StunUpdateJob()
            {
                Commands = commands,
                CurrentTime = timeSystem.GameTime,
                StunModificatorEntity = stunModificatorEntity,
                SpeedModLookup = speedModsLookup,
                AbilityBlockerLookup = abilityBlockerLookup
            };

            Dependency = stunUpdateJob.Schedule(Dependency);

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
                StunDuration duration = default;

                for(int c=0; c<StunRequests.Length; c++)
                {
                    var chunk = StunRequests[c];
                    var reqBuffers = chunk.GetBufferAccessor(ref HitType);
                    var requests = chunk.GetNativeArray(ref RequestType);

                    for(int i=0; i<requests.Length; i++)
                    {
                        var hits = reqBuffers[i];

                        if(ProcessTarget(index, entity, hits, requests[i], out Stunned newStunned, out var newStunDuration))
                        {
                            if(added)
                            {
                                if(newStunDuration.Value > duration.Value)
                                {
                                    duration.Value = newStunDuration.Value;
                                    stunned = newStunned;
                                }
                            }
                            else
                            {
                                added = true;
                                stunned = newStunned;
                                duration = newStunDuration;
                            }
                        }
                    }
                }

                if(added)
                {
                    Commands.AddComponent(index, entity, stunned);
                    Commands.AddComponent(index, entity, duration);
                }
            }

            public bool ProcessTarget(int index, Entity hitTarget, DynamicBuffer<HitBufferElement> hits, StunRequest stun, out Stunned stunned, out StunDuration stunDuration)
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
                    };
                    stunDuration = new StunDuration
                    {
                        Value = stun.Duration
                    };

                    return true;
                }
                stunned = default;
                stunDuration = default;
                return false;
            }
        }
        [BurstCompile]

        partial struct StunUpdateJob : IJobEntity
        {
            public Entity StunModificatorEntity;
            public double CurrentTime;
            public EntityCommandBuffer.ParallelWriter Commands;
            public BufferLookup<SpeedModificator> SpeedModLookup;
            public BufferLookup<CharacterAbilityBlocker> AbilityBlockerLookup;

            public void Execute(Entity entity, [ChunkIndexInQuery] int index, ref Stunned stunned, in StunDuration stunDuration)
            {
                if(stunned.PendingStart)
                {
                    stunned.StartTime = CurrentTime;
                    stunned.PendingStart = false;
                    stunned.ModificatorOwner = StunModificatorEntity;

                    if (SpeedModLookup.TryGetBuffer(entity, out var speedMods))
                    {
                        var speedModificator = new SpeedModificator
                        {
                            Value = new CharacteristicModificator
                            {
                                Value = 0,
                                Operator = ModificatorOperators.MULTIPLY_ACTUAL
                            },
                            Owner = StunModificatorEntity
                        };
                        speedMods.Add(speedModificator);
                    }

                    if (AbilityBlockerLookup.TryGetBuffer(entity, out var abilityBlockers))
                    {
                        var blockModificator = new CharacterAbilityBlocker
                        {
                            Entity = StunModificatorEntity,
                            ForceStopCurrentAbility = true
                        };
                        abilityBlockers.Add(blockModificator);    
                    }
                }

                if(CurrentTime - stunned.StartTime >= stunDuration.Value || stunned.PendingFinish)
                {
                    Commands.RemoveComponent<Stunned>(index, entity);
                    Commands.RemoveComponent<StunDuration>(index, entity);
                    
                    if (SpeedModLookup.TryGetBuffer(entity, out var speedMods))
                    {
                        IOwnedModificatorExtensions.RemoveModificatorsWithOwner(StunModificatorEntity, speedMods);
                    }

                    if (AbilityBlockerLookup.TryGetBuffer(entity, out var abilityBlockers))
                    {
                        CharacterAbilityBlocker.RemoveFromBuffer(StunModificatorEntity, abilityBlockers);
                    }
                }
            }
        }
    }
}
