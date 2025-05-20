using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MultiplayerKit;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Sync]
    public struct DifficultyData : IComponentData
    {
        public ushort EnemyStrengthMultiplier;
    }
    
    [DisableAutoCreation]
    public partial class DifficultySystem : GameSystemBase
    {
        private EntityQuery changedPlayersQuery;
        private EntityQuery allPlayersQuery;
        private bool isAuthority = true;
        private EntityQuery healthModQuery;
        private EntityQuery healthModOnlyChangedQuery;
        private EntityQuery changedDifficultyDataQuery;

        public DifficultySystem(bool isAuthority)
        {
            this.isAuthority = isAuthority;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            allPlayersQuery = GetEntityQuery(ComponentType.ReadOnly<AuthorizedUser>());
            
            var healthModDesc = new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<HealthModificator>(),
                    ComponentType.ReadWrite<Health>(),
                    ComponentType.ReadWrite<Enemy>(),
                }
            };
            
            healthModQuery = GetEntityQuery(healthModDesc);
            healthModOnlyChangedQuery = GetEntityQuery(healthModDesc);
            healthModOnlyChangedQuery.AddChangedVersionFilter(typeof(Enemy));

            if (isAuthority == false)
            {
                changedDifficultyDataQuery = GetEntityQuery(ComponentType.ReadOnly<DifficultyData>());   
                changedDifficultyDataQuery.AddChangedVersionFilter(typeof(DifficultyData));
            }
        }

        protected override void OnSystemUpdate()
        {
            if (isAuthority)
            {
                if (updateAuthority(out DifficultyData diffData))
                {
                    var job = new UpdateEnemiesJob
                    {
                        ModificatorEntity = SystemAPI.GetSingletonEntity<DifficultyData>(),
                        DifficultyData = diffData
                    };
                    job.Run(healthModQuery);
                }
                else
                {
                    var job = new UpdateEnemiesJob
                    {
                        ModificatorEntity = SystemAPI.GetSingletonEntity<DifficultyData>(),
                        DifficultyData = diffData
                    };
                    job.Run(healthModOnlyChangedQuery);
                }
            }
            else
            {
                if (SystemAPI.TryGetSingleton(out DifficultyData difficultyData))
                {
                    var job = new UpdateEnemiesJob
                    {
                        ModificatorEntity = SystemAPI.GetSingletonEntity<DifficultyData>(),
                        DifficultyData = difficultyData
                    };

                    if (changedDifficultyDataQuery.IsEmpty)
                    {
                        job.Run(healthModOnlyChangedQuery);    
                    }
                    else
                    {
                        job.Run(healthModQuery);
                    }
                }
            }
        }

        [BurstCompile]
        partial struct UpdateEnemiesJob : IJobEntity
        {
            public DifficultyData DifficultyData;
            public Entity ModificatorEntity;
            
            public void Execute(ref DynamicBuffer<HealthModificator> healthMods, ref Health health)
            {
                if (IOwnedModificatorExtensions.TryGet(ModificatorEntity, healthMods, out var addedMod))
                {
                    health.ModifiedHP /= addedMod.Value.Value;
                    IOwnedModificatorExtensions.RemoveModificatorsWithOwner(ModificatorEntity, healthMods);
                }
                
                Debug.Log($"Change health of entity for difficulty: {DifficultyData.EnemyStrengthMultiplier}");
                var mod = new CharacteristicModificator
                {
                    Operator = ModificatorOperators.MULTIPLY_ACTUAL,
                    Value = DifficultyData.EnemyStrengthMultiplier
                };
                healthMods.Add(new HealthModificator { Value = mod, Owner = ModificatorEntity });
                
                // это значение должно быть уменьшено до макс значения в системе модификаторов
                health.ActualHP = health.ModifiedHP * mod.Value;
            }
        }

        bool updateAuthority(out DifficultyData diffData)
        {
            bool changed = false;
            
            if (SystemAPI.HasSingleton<DifficultyData>() == false)
            {
                EntityManager.CreateEntity(ComponentType.ReadOnly<DifficultyData>(), ComponentType.ReadOnly<NetworkID>());
                SystemAPI.SetSingleton(new DifficultyData { EnemyStrengthMultiplier = 1 });
                changed = true;
            }
            
            var playerCount = allPlayersQuery.CalculateEntityCount();
            
            diffData = SystemAPI.GetSingleton<DifficultyData>();
            
            if (diffData.EnemyStrengthMultiplier != playerCount)
            {
                Debug.Log($"Difficulty change for players: {playerCount}");
                diffData = new DifficultyData { EnemyStrengthMultiplier = (ushort)playerCount };
                SystemAPI.SetSingleton(diffData);
                changed = true;
            }

            return changed;
        }
    }
}
