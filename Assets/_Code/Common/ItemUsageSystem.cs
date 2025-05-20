using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(UseItemRequestSystem))]
    public partial class ItemUsageCheckSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<ItemUsageSystem.Singleton>();
        }

        protected override void OnUpdate()
        {
            var systemSingleton = SystemAPI.GetSingletonEntity<ItemUsageSystem.Singleton>();
            
            Entities
                .ForEach((Entity entity, ref UseItemRequest useRequest) =>
                {
                    if (useRequest.Status != UseRequestStatus.Pending)
                    {
                        return;
                    }

                    if (SystemAPI.HasBuffer<HealthRegenModificator>(useRequest.ItemEntity))
                    {
                        Entity itemOwner = Entity.Null;

                        if (SystemAPI.HasComponent<Target>(entity))
                        {
                            var request = SystemAPI.GetComponent<Target>(entity);
                            itemOwner = request.Value;
                        }
                        else
                        {
                            var item = SystemAPI.GetComponent<Item>(useRequest.ItemEntity);
                            itemOwner = item.Owner;
                        }

                        if (itemOwner != Entity.Null)
                        {
                            var ownerMods = SystemAPI.GetBuffer<HealthRegenModificator>(itemOwner);

                            foreach (var ownerMod in ownerMods)
                            {
                                if (ownerMod.Owner == systemSingleton)
                                {
                                    Debug.Log("Item usage request failed - health regen already applied");
                                    useRequest.Status = UseRequestStatus.Failed;
                                    break;
                                }
                            }
                        }
                    }
                    
                }).Run();
        }
    }
    
    [DisableAutoCreation]
    [UpdateAfter(typeof(ProcessUsedItemRequestSystem))]
    public partial class ItemUsageSystem : GameSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            EntityManager.AddComponentData(SystemHandle, new Singleton());
        }

        public struct Singleton : IComponentData
        {
        }

        [Serializable]
        public struct TimeModificator : IComponentData
        {
            public double StartTime;
            public double Duration;
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            var modifyHealthArchetype = SystemAPI.GetSingleton<ModifyHealthSystem.Singleton>().ModifyEventArchetype;
            var systemEntity = SystemAPI.GetSingletonEntity<Singleton>();
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            
            // обработка событий использования предметов
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, in UseItemRequest useRequest) =>
                {
                    if (useRequest.Status != UseRequestStatus.Success)
                    {
                        return;
                    }

                    var itemEntity = useRequest.ItemEntity;

                    if (SystemAPI.HasComponent<Item>(itemEntity) == false)
                    {
                        return;
                    }

                    Entity itemOwner = Entity.Null;

                    if (SystemAPI.HasComponent<Target>(entity))
                    {
                        var request = SystemAPI.GetComponent<Target>(entity);
                        itemOwner = request.Value;
                    }
                    else
                    {
                        var item = SystemAPI.GetComponent<Item>(itemEntity);
                        itemOwner = item.Owner;
                    }
                    
                    if (SystemAPI.HasComponent<ModifyHealth>(itemEntity) && itemOwner != Entity.Null)
                    {
                        var modify = SystemAPI.GetComponent<ModifyHealth>(itemEntity);
                        var modifyRequest = commands.CreateEntity(entityInQueryIndex, modifyHealthArchetype);
                        commands.SetComponent(entityInQueryIndex, modifyRequest, modify);
                        commands.SetComponent(entityInQueryIndex, modifyRequest, new Target { Value = itemOwner });
                    }
                    else if(SystemAPI.HasBuffer<HealthRegenModificator>(itemEntity) && itemOwner != Entity.Null 
                            && SystemAPI.HasBuffer<HealthRegenModificator>(itemOwner))
                    {
                        var modificators = SystemAPI.GetBuffer<HealthRegenModificator>(itemEntity);

                        if (modificators.Length > 0)
                        {
                            var ownerModificators = SystemAPI.GetBuffer<HealthRegenModificator>(itemOwner);
                            
                            if (IOwnedModificatorExtensions.TryGet(systemEntity, in ownerModificators, out _) == false)
                            {
                                var newMod = modificators[0];
                                newMod.Owner = systemEntity;
                                ownerModificators.Add(newMod);

                                var modOwnerEntity = commands.CreateEntity(entityInQueryIndex);
                                commands.AddComponent(entityInQueryIndex, modOwnerEntity, new TimeModificator
                                {
                                    StartTime = elapsedTime,
                                    Duration = 15,
                                });
                                commands.AddComponent(entityInQueryIndex, modOwnerEntity, new Target(itemOwner));
                                commands.AddComponent(entityInQueryIndex, modOwnerEntity, new Owner(systemEntity));
                            }    
                        }
                    }

                })
                .Run();
            
            
            Entities.ForEach((Entity timeModEntity, int entityInQueryIndex, in Target target, in Owner owner, in TimeModificator timeModificator) =>
            {
                if (elapsedTime - timeModificator.StartTime < timeModificator.Duration)
                {
                    return;
                }
                commands.DestroyEntity(entityInQueryIndex, timeModEntity);

                if (SystemAPI.HasBuffer<HealthRegenModificator>(target.Value))
                {
                    var targetMods = SystemAPI.GetBuffer<HealthRegenModificator>(target.Value);
                    IOwnedModificatorExtensions.RemoveModificatorsWithOwner(owner.Value, in targetMods);
                    Debug.Log($"removed health regen modificator with owner {owner.Value} from {target.Value.Index}");
                }
                
            }).Run();
        }
    }
}
