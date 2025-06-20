using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    public struct AddAbilityPointRequest : IComponentData
    {
        public Entity Character;
        public ushort Count;
    }

    [Serializable]
    public struct AbilityPointChangedEvent : IComponentData
    {
        public Entity Character;
    }
    
    [Serializable]
    public struct LearnAbilityRequest : IComponentData
    {
        public AbilityID AbilityID;
    }

    [Serializable]
    public struct AbilityLearnedEvent : IComponentData
    {
        public Entity Character;
    }

    [Serializable]
    public struct ActivateAbilityRequest : IComponentData
    {
        public AbilityID AbilityID;
        public byte Slot;
    }

    [Serializable]
    public struct AbilityActivatedEvent : IComponentData
    {
        public Entity Character;
    }
    
    /// <summary>
    /// система для обработки запросов на обучение умениям и апгрейдам характеристик
    /// </summary>
    [BurstCompile]
    public partial struct CharacterLearnSystem : ISystem
    {
        private EntityQuery learnAbilityRequestQuery;
        private EntityQuery activateAblilityRequestQuery;
        private ComponentLookup<AbilityPoints> abilityPointsLookup;
        
        public void OnCreate(ref SystemState state)
        {
            learnAbilityRequestQuery = state.GetEntityQuery( new []
            {
                ComponentType.ReadOnly<LearnAbilityRequest>(),
                ComponentType.ReadOnly<Target>(),
            });
            activateAblilityRequestQuery = state.GetEntityQuery( new []
            {
                ComponentType.ReadOnly<ActivateAbilityRequest>(),
                ComponentType.ReadOnly<Target>(),
            });
            
            state.RequireForUpdate<MainDatabaseTag>();
            state.RequireForUpdate<GameCommandBufferSystem.Singleton>();

            abilityPointsLookup = state.GetComponentLookup<AbilityPoints>();
        }

        public void OnUpdate(ref SystemState state)
        {
            abilityPointsLookup.Update(ref state);
            
            var commands = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            new AddAbilityPointJob
            {
                Commands = commands,
                AbilityPointsLookup = abilityPointsLookup
            }.Run();

            new ProcessLevelUpEventJob
            {
                Commands = commands,
                AbilityPointsLookup = abilityPointsLookup
            }.Run();
            
            if (learnAbilityRequestQuery.IsEmpty == false)
            {
                ProcessLearnAbilityRequests(ref state, commands);
            }

            if (activateAblilityRequestQuery.IsEmpty == false)
            {
                ProcessActivateAbilitiesRequest(ref state, commands);
            }
        }
        
        [BurstCompile]
        partial struct ProcessLevelUpEventJob : IJobEntity
        {
            public EntityCommandBuffer Commands;
            public ComponentLookup<AbilityPoints> AbilityPointsLookup;
            
            public void Execute(Entity entity, in LevelUpEventData eventData)
            {
                if (eventData.CurrentLevel <= eventData.PreviousLevel)
                {
                    return;
                }
                var points = AbilityPointsLookup.GetRefRWOptional(eventData.Target);

                if (points.IsValid == false)
                {
                    return;
                }
                #if UNITY_EDITOR
                Debug.Log("Added ability point for level up");
                #endif
                points.ValueRW.Count += (ushort)(eventData.CurrentLevel - eventData.PreviousLevel);

                var eventEntity = Commands.CreateEntity();
                Commands.AddComponent(eventEntity, new AbilityPointChangedEvent
                {
                    Character = eventData.Target
                });
                Commands.AddComponent(eventEntity, new EventTag());
            }
        }

        private void ProcessActivateAbilitiesRequest(ref SystemState state, EntityCommandBuffer commands)
        {
            var requests = activateAblilityRequestQuery.ToComponentDataArray<ActivateAbilityRequest>(Allocator.Temp);
            var targets = activateAblilityRequestQuery.ToComponentDataArray<Target>(Allocator.Temp);

            foreach (var (playerAbilitiesRW, abilities, entity) 
                     in SystemAPI.Query<RefRW<PlayerAbilities>, DynamicBuffer<AbilityElement>>()
                         .WithAll<PlayerController>().WithEntityAccess())
            {
                int requestIndex = -1;

                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];

                    if (target.Value == entity)
                    {
                        requestIndex = i;
                        break;
                    }
                }
                if (requestIndex == -1)
                {
                    continue;
                }
                
                var request = requests[requestIndex];
                ref var playerAbilities = ref playerAbilitiesRW.ValueRW;

                foreach (var ability in abilities)
                {
                    var abilityID = SystemAPI.GetComponent<AbilityID>(ability.AbilityEntity);
                    var abilityInfo = new PlayerAbilityInfo
                    {
                        Ability = ability.AbilityEntity,
                        ID = abilityID
                    };

                    if (abilityID == request.AbilityID)
                    {
                        switch (request.Slot)
                        {
                            case 0:
                                playerAbilities.AttackAbility = abilityInfo;
                                break;
                            case 1:
                                playerAbilities.Ability1 = abilityInfo;
                                break;
                            case 2:
                                playerAbilities.Ability2 = abilityInfo;
                                break;
                            case 3:
                                playerAbilities.Ability3 = abilityInfo;
                                break;
                        }
                        break;
                    }
                }
                var eventEntity = commands.CreateEntity();
                commands.AddComponent(eventEntity, new AbilityActivatedEvent
                {
                    Character = entity
                });
                commands.AddComponent(eventEntity, new EventTag());
            }
            
            state.EntityManager.DestroyEntity(activateAblilityRequestQuery);
        }

        [BurstCompile]
        partial struct AddAbilityPointJob : IJobEntity
        {
            public EntityCommandBuffer Commands;
            public ComponentLookup<AbilityPoints> AbilityPointsLookup;
            
            public void Execute(Entity entity, in AddAbilityPointRequest request)
            {
                Commands.DestroyEntity(entity);
                
                var pointsRW = AbilityPointsLookup.GetRefRWOptional(request.Character);
                
                if (pointsRW.IsValid == false)
                {
                    return;
                }
                pointsRW.ValueRW.Count += request.Count;
                
                var pointEventEntity = Commands.CreateEntity();
                Commands.AddComponent(pointEventEntity, new AbilityPointChangedEvent
                {
                    Character = request.Character
                });
                Commands.AddComponent(pointEventEntity, new EventTag());
            }
        }
        
        private void ProcessLearnAbilityRequests(ref SystemState state, EntityCommandBuffer commands)
        {
            var mainDB_entity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var mainDB = SystemAPI.GetBuffer<IdToEntity>(mainDB_entity);

            var requests = learnAbilityRequestQuery.ToComponentDataArray<LearnAbilityRequest>(Allocator.Temp);
            var targets = learnAbilityRequestQuery.ToComponentDataArray<Target>(Allocator.Temp);

            foreach (var (abilityPrefabs, abilityPointsRW, level, entity) 
                     in SystemAPI.Query<
                             DynamicBuffer<AbilityPrefabElement>,
                             RefRW<AbilityPoints>,
                             RefRO<Level>
                         >().WithAll<PlayerController>().WithEntityAccess())
            {
                int requestIndex = -1;

                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];

                    if (target.Value == entity)
                    {
                        requestIndex = i;
                        break;
                    }
                }

                if (requestIndex == -1)
                {
                    continue;
                }

                var request = requests[requestIndex];

                ref var abilityPoints = ref abilityPointsRW.ValueRW;

                if (abilityPoints.Count == 0)
                {
                    Debug.LogError("not enough ability points");
                    continue;
                }

                var prefab = IdToEntity.GetEntityByID(in mainDB, request.AbilityID.Value);

                if (prefab != Entity.Null)
                {
#if UNITY_EDITOR
                    Debug.Log($"Learn ability with id {request.AbilityID.Value}");
#endif
                    abilityPoints.Count--;
                    
                    var pointEventEntity = commands.CreateEntity();
                    commands.AddComponent(pointEventEntity, new AbilityPointChangedEvent
                    {
                        Character = entity
                    });
                    commands.AddComponent(pointEventEntity, new EventTag());
                    
                    abilityPrefabs.Add(new AbilityPrefabElement
                    {
                        Value = prefab
                    });

                    var eventEntity = commands.CreateEntity();
                    commands.AddComponent(eventEntity, new AbilityLearnedEvent
                    {
                        Character = entity
                    });
                    commands.AddComponent(eventEntity, new EventTag());
                    break;
                }
            }

            commands.DestroyEntity(learnAbilityRequestQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
