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
    public struct LearnAbilityRequest : IBufferElementData
    {
        public AbilityID AbilityID;
        public byte Points;
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
        private EntityQuery activateAblilityRequestQuery;
        private ComponentLookup<AbilityPoints> abilityPointsLookup;
        private ComponentLookup<AbilityID> abilityIdLookup;
        private ComponentLookup<MaximumLevel> maxLevelLookup;
        private ComponentLookup<Level> levelLookup;
        private ComponentLookup<MinimalLevel> minimalLevelLookup;
        private BufferLookup<AbilityArray> abilitiesLookup;
        
        public void OnCreate(ref SystemState state)
        {
            activateAblilityRequestQuery = state.GetEntityQuery( new []
            {
                ComponentType.ReadOnly<ActivateAbilityRequest>(),
                ComponentType.ReadOnly<Target>(),
            });
            
            state.RequireForUpdate<MainDatabaseTag>();
            state.RequireForUpdate<GameCommandBufferSystem.Singleton>();

            abilityPointsLookup = state.GetComponentLookup<AbilityPoints>();
            abilitiesLookup = state.GetBufferLookup<AbilityArray>(true);
            maxLevelLookup = state.GetComponentLookup<MaximumLevel>(true);
            minimalLevelLookup = state.GetComponentLookup<MinimalLevel>(true);
            abilityIdLookup = state.GetComponentLookup<AbilityID>(true);
            levelLookup = state.GetComponentLookup<Level>(true);
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
            
            
            var mainDB_entity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var mainDB = SystemAPI.GetBuffer<IdToEntity>(mainDB_entity);
            
            abilitiesLookup.Update(ref state);
            abilityIdLookup.Update(ref state);
            minimalLevelLookup.Update(ref state);
            maxLevelLookup.Update(ref state);
            levelLookup.Update(ref state);
            
            new LearnAbilityRequestsJob
            {
                PrefabDatabase = mainDB.AsNativeArray(),
                Commands = commands,
                AbilityPointsLookup = abilityPointsLookup,
                AbilitiesLookup = abilitiesLookup,
                AbilityIdLookup = abilityIdLookup,
                MinimalLevelLookup = minimalLevelLookup,
                MaximumLevelLookup = maxLevelLookup, 
                LevelLookup = levelLookup
            }.Run();

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
                     in SystemAPI.Query<RefRW<PlayerAbilities>, DynamicBuffer<TzarGames.GameCore.Abilities.AbilityArray>>()
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

        [BurstCompile]
        partial struct LearnAbilityRequestsJob : IJobEntity
        {
            [ReadOnly] public NativeArray<IdToEntity> PrefabDatabase;
            public ComponentLookup<AbilityPoints> AbilityPointsLookup;
            [ReadOnly] public ComponentLookup<Level> LevelLookup;
            [ReadOnly] public ComponentLookup<AbilityID> AbilityIdLookup;
            [ReadOnly] public BufferLookup<AbilityArray> AbilitiesLookup;
            [ReadOnly] public ComponentLookup<MinimalLevel> MinimalLevelLookup;
            [ReadOnly] public ComponentLookup<MaximumLevel> MaximumLevelLookup;
            public EntityCommandBuffer Commands;

            struct AbilityLearnInfo
            {
                public Entity Prefab;
                public Entity Instance;
                public Level InstanceLevel;
                public LearnAbilityRequest Request;
            }
            
            public void Execute(Entity entity, in DynamicBuffer<LearnAbilityRequest> requests, in Target target)
            {
                Commands.DestroyEntity(entity);
                
                var abilityPointsRW = AbilityPointsLookup.GetRefRWOptional(target.Value);
                if (abilityPointsRW.IsValid == false)
                {
                    Debug.LogError($"Failed to learn abilities, invalid target {target.Value.Index}");
                    return;
                }

                int requiredPoints = 0;

                foreach (var request in requests)
                {
                    requiredPoints += request.Points;
                }

                if (abilityPointsRW.ValueRW.Count < requiredPoints)
                {
                    Debug.LogError($"Failed to learn abilities, not enough ability points on {target.Value.Index}");
                    return;
                }

                if (AbilitiesLookup.TryGetBuffer(target.Value, out var abilities) == false)
                {
                    Debug.LogError($"Failed to learn abilities, target {target.Value.Index} has no abilities array");
                    return;
                }

                if (LevelLookup.TryGetComponent(target.Value, out var targetLevel) == false)
                {
                    Debug.LogError("Failed to learn abilities, target has no level component");
                    return;
                }
                var learnList = new NativeList<AbilityLearnInfo>(requests.Length, Allocator.Temp);

                foreach (var request in requests)
                {
                    if (request.Points == 0)
                    {
                        Debug.LogError($"Failed to learn ability, request for ability {request.AbilityID.Value} has 0 points");
                        return;
                    }
                    var prefab = IdToEntity.GetEntityByID(in PrefabDatabase, request.AbilityID.Value);

                    if (prefab == Entity.Null)
                    {
                        Debug.LogError($"Failed to learn ability {request.AbilityID.Value}, no prefab found");
                        return;
                    }

                    if (MinimalLevelLookup.TryGetComponent(prefab, out var minimalLevel) == false)
                    {
                        Debug.LogError($"Failed to learn abilities, ability prefab {prefab.Index} has no minimal level component");
                        return;
                    }

                    if (minimalLevel.Value > targetLevel.Value)
                    {
                        Debug.LogError($"Failed to learn abilities, target {target.Value.Index} level {targetLevel.Value} less than required {minimalLevel.Value} from ability prefab {prefab.Index}");
                        return;
                    }

                    Entity instance = Entity.Null;
                    Level instanceLevel = default;
                    
                    foreach (var ability in abilities)
                    {
                        if (AbilityIdLookup.TryGetComponent(ability.AbilityEntity, out var id) == false)
                        {
                            Debug.LogError($"Failed to learn abilities, player ability {ability.AbilityEntity.Index} has no ID");
                            return;
                        }

                        if (LevelLookup.TryGetComponent(ability.AbilityEntity, out var level) == false)
                        {
                            Debug.LogError($"Failed to learn abilities, player ability {ability.AbilityEntity.Index} has no level");
                            return;
                        }

                        if (id == request.AbilityID)
                        {
                            instance = ability.AbilityEntity;
                            instanceLevel = level;
                            break;
                        }
                    }
                    
                    if (MaximumLevelLookup.TryGetComponent(prefab, out var maxLevel) == false)
                    {
                        Debug.LogError($"Failed to learn abilities, ability prefab {prefab.Index} with id {request.AbilityID.Value} has no max level component");
                        return;
                    }
                    
                    int requestedLevel = request.Points + instanceLevel.Value;

                    if (requestedLevel > maxLevel.Value)
                    {
                        Debug.LogError($"Failed to learn abilities, max level cap on ability {instance.Index} (max {maxLevel.Value}) with level {instanceLevel.Value} and required points to add: {request.Points}");
                        return;
                    }
                    
                    learnList.Add(new AbilityLearnInfo
                    {
                        Prefab = prefab,
                        Request = request,
                        Instance = instance,
                        InstanceLevel = instanceLevel
                    });
                }
                
                foreach (var request in learnList)
                {
                    if (request.Instance != Entity.Null)
                    {
                        var instanceLevel = request.InstanceLevel;
                        instanceLevel.Value += request.Request.Points;
                        
                        Commands.SetComponent(request.Instance, instanceLevel);
                    }
                    else
                    {
                        var instance = Commands.Instantiate(request.Prefab);
                        Commands.AppendToBuffer(target.Value, new AbilityArray
                        {
                            AbilityEntity = instance
                        });
                        Commands.SetComponent(instance, new AbilityOwner { Value = target.Value });
                        Commands.SetComponent(instance, new Level
                        {
                            Value = request.Request.Points
                        });
                    }
                }

                abilityPointsRW.ValueRW.Count -= (ushort)requiredPoints;
                
                var pointEventEntity = Commands.CreateEntity();
                Commands.AddComponent(pointEventEntity, new AbilityPointChangedEvent
                {
                    Character = target.Value
                });
                Commands.AddComponent(pointEventEntity, new EventTag());
                
                var eventEntity = Commands.CreateEntity();
                Commands.AddComponent(eventEntity, new AbilityLearnedEvent
                {
                    Character = target.Value
                });
                Commands.AddComponent(eventEntity, new EventTag());
                
                #if UNITY_EDITOR
                Debug.Log("Ability learn success");
                #endif
            }
        }
    }
}
