using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(ProcessUsedItemRequestSystem))]
    [UpdateInGroup(typeof(PreSimulationSystemGroup))]
    public partial class ItemUsageSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();
            var modifyHealthArchetype = SystemAPI.GetSingleton<ModifyHealthSystem.Singleton>().ModifyEventArchetype;
            
            // обработка событий использования предметов
            Entities
                .WithChangeFilter<UseItemRequest>()
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

                })
#if TZAR_GAMECORE_THREADS
                .Schedule();
#else
                .Run();
#endif
        }
    }
}
