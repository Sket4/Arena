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
    public struct LearnAbilityRequest : IComponentData
    {
        public AbilityID AbilityID;
    }

    [Serializable]
    public struct ActivateAbilityRequest : IComponentData
    {
        public AbilityID AbilityID;
        public byte Slot;
    }
    
    /// <summary>
    /// система для обработки запросов на обучение умениям и апгрейдам характеристик
    /// </summary>
    [BurstCompile]
    public partial struct CharacterLearnSystem : ISystem
    {
        private EntityQuery learnAbilityRequestQuery;
        private EntityQuery activateAblilityRequestQuery;
        
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
        }

        public void OnUpdate(ref SystemState state)
        {
            if (learnAbilityRequestQuery.IsEmpty == false)
            {
                ProcessLearAbilityRequests(ref state);
            }

            if (activateAblilityRequestQuery.IsEmpty == false)
            {
                ProcessActivateAbilitiesRequest(ref state);
            }
        }

        private void ProcessActivateAbilitiesRequest(ref SystemState state)
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
            }
            
            state.EntityManager.DestroyEntity(activateAblilityRequestQuery);
        }

        private void ProcessLearAbilityRequests(ref SystemState state)
        {
            var mainDB_entity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var mainDB = SystemAPI.GetBuffer<IdToEntity>(mainDB_entity);

            var requests = learnAbilityRequestQuery.ToComponentDataArray<LearnAbilityRequest>(Allocator.Temp);
            var targets = learnAbilityRequestQuery.ToComponentDataArray<Target>(Allocator.Temp);

            foreach (var (abilityPrefabs, entity) in SystemAPI.Query<DynamicBuffer<AbilityPrefabElement>>()
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

                var prefab = IdToEntity.GetEntityByID(in mainDB, request.AbilityID.Value);

                if (prefab != Entity.Null)
                {
#if UNITY_EDITOR
                    Debug.Log($"Learn ability with id {request.AbilityID.Value}");
#endif
                    abilityPrefabs.Add(new AbilityPrefabElement
                    {
                        Value = prefab
                    });
                    break;
                }
            }

            state.EntityManager.DestroyEntity(learnAbilityRequestQuery);
        }
    }
}
