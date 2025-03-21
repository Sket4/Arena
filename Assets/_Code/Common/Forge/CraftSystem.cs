using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    public enum CraftReceiptState : byte
    {
        Pending,
        Processing,
        NotEnoughItems,
        InvalidInventoryTransaction,
        InvalidCrafter,
        CrafterUnavailable,
        CrafterDoesNotHaveRequiredReceipt,
        Success
    };
    
    public struct CraftRequest : IComponentData
    {
        public Entity InventoryOwner;
        public Entity Receipt;
        public Entity Crafter;
        public CraftReceiptState State;
    }

    struct CraftInventoryTransaction : ICleanupComponentData
    {
        public Entity Transaction;
    }

    [DisableAutoCreation]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class CraftSystem : GameSystemBase
    {
        private EntityArchetype transactionArchetype;

        protected override void OnCreate()
        {
            base.OnCreate();
            transactionArchetype = EntityManager.CreateArchetype(typeof(InventoryTransaction), typeof(ItemsToRemove), typeof(ItemsToAdd), typeof(Target));
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            var transactionArchetype = this.transactionArchetype;
            var craftReceiptsFromEntity = GetBufferLookup<CraftReceipts>(true);
            
            Entities
                .WithoutBurst()
                .WithReadOnly(craftReceiptsFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref CraftRequest request) =>
                {
                    if (request.State != CraftReceiptState.Pending)
                    {
                        if (request.State == CraftReceiptState.Processing)
                        {
                            if (SystemAPI.HasComponent<CraftInventoryTransaction>(entity))
                            {
                                var transactionData = SystemAPI.GetComponent<CraftInventoryTransaction>(entity);

                                if (SystemAPI.HasComponent<InventoryTransaction>(transactionData.Transaction))
                                {
                                    var transaction = SystemAPI.GetComponent<InventoryTransaction>(transactionData.Transaction);
                                    
                                    switch (transaction.Status)
                                    {
                                        case InventoryTransactionStatus.Processing:
                                            break;
                                        case InventoryTransactionStatus.Failed:
                                            request.State = CraftReceiptState.InvalidInventoryTransaction;
                                            break;
                                        case InventoryTransactionStatus.Success:
                                            request.State = CraftReceiptState.Success;
                                            break;
                                    }
                                }
                                else
                                {
                                    request.State = CraftReceiptState.InvalidInventoryTransaction;
                                } 
                            }
                        }
                        return;
                    }

                    if (craftReceiptsFromEntity.HasBuffer(request.Crafter) == false)
                    {
                        request.State = CraftReceiptState.InvalidCrafter;
                        return;
                    }

                    var crafterReceipts = craftReceiptsFromEntity[request.Crafter];

                    bool hasReceipt = false;
                    
                    foreach (var crafterReceipt in crafterReceipts)
                    {
                        if (crafterReceipt.Receipt == request.Receipt)
                        {
                            hasReceipt = true;
                            break;
                        }
                    }

                    if (hasReceipt == false)
                    {
                        request.State = CraftReceiptState.CrafterDoesNotHaveRequiredReceipt;
                        return;
                    }

                    request.State = CraftReceiptState.Processing;

                    var requiredItems = SystemAPI.GetBuffer<CraftReceiptItems>(request.Receipt);
                    var inventory = SystemAPI.GetBuffer<InventoryElement>(request.InventoryOwner);

                    bool hasAllRequiredItems = true;
                    
                    foreach (var requiredItem in requiredItems)
                    {
                        var requiredItemData = SystemAPI.GetComponent<Item>(requiredItem.Item);
                        uint itemCount = 0;
                        
                        foreach (var item in inventory)
                        {
                            var itemData = SystemAPI.GetComponent<Item>(item.Entity);
                            if (itemData.ID == requiredItemData.ID)
                            {
                                if (SystemAPI.HasComponent<Consumable>(item.Entity))
                                {
                                    itemCount += SystemAPI.GetComponent<Consumable>(item.Entity).Count;    
                                }
                                else
                                {
                                    itemCount++;
                                }
                            }
                        }

                        if (itemCount == 0 || itemCount < requiredItem.Count)
                        {
                            hasAllRequiredItems = false;
                            break;
                        }
                    }

                    if (hasAllRequiredItems == false)
                    {
                        request.State = CraftReceiptState.NotEnoughItems;
                        return;
                    }

                    var transactionEntity = commands.CreateEntity(entityInQueryIndex, transactionArchetype);
                    commands.SetComponent(entityInQueryIndex, transactionEntity, new Target
                    {
                        Value = request.InventoryOwner
                    });

                    var toRemove = commands.SetBuffer<ItemsToRemove>(entityInQueryIndex, transactionEntity);

                    foreach (var requiredItem in requiredItems)
                    {
                        var requiredItemData = SystemAPI.GetComponent<Item>(requiredItem.Item);
                        
                        foreach (var item in inventory)
                        {
                            var itemData = SystemAPI.GetComponent<Item>(item.Entity);
                            if (itemData.ID == requiredItemData.ID)
                            {
                                toRemove.Add(new ItemsToRemove(item.Entity, requiredItem.Count));
                            }
                        }
                    }

                    var toAdd = commands.SetBuffer<ItemsToAdd>(entityInQueryIndex, transactionEntity);
                    var receipt = SystemAPI.GetComponent<CraftReceipt>(request.Receipt);
                    var itemInstance = commands.Instantiate(entityInQueryIndex, receipt.Item);

                    toAdd.Add(new ItemsToAdd(itemInstance));

                    commands.AddComponent(entityInQueryIndex, entity, new CraftInventoryTransaction
                    {
                        Transaction = transactionEntity
                    });

                })
#if TZAR_GAMECORE_THREADS
                .Schedule();
#else
                .Run();        
#endif

            Entities
                .WithNone<CraftRequest>()
                .ForEach((Entity entity, int entityInQueryIndex, ref CraftInventoryTransaction transactionState) =>
                {
                    commands.RemoveComponent<CraftInventoryTransaction>(entityInQueryIndex, entity);

                })
#if TZAR_GAMECORE_THREADS
                .Schedule();
#else
                .Run();        
#endif
        }
    }
}
