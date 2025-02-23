using Arena.Items;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(InventorySystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class StoreSystem : GameSystemBase, IRpcProcessor
    {
        private EntityQuery storeNetIdsQuery;
        public EntityArchetype inventoryEventArchetype;

        public NetworkIdentity NetIdentity { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            inventoryEventArchetype = EntityManager.CreateArchetype(
                typeof(InventoryTransaction), 
                typeof(Target),
                typeof(ItemsToAdd), 
                typeof(ItemsToRemove));
            
            storeNetIdsQuery = GetEntityQuery(ComponentType.ReadOnly<StoreItems>(), ComponentType.ReadOnly<NetworkID>());
        }

        protected override void OnSystemUpdate()
        {
            bool isServer = NetIdentity != null && NetIdentity.Net != null && NetIdentity.Net.IsServer;
            
            if(NetIdentity == null || isServer)
            {
                updateAuthority(isServer);
            }
        }

        private void updateAuthority(bool isServer)
        {
            var commands = CreateUniversalCommandBuffer();
            processPurchaseRequests(commands);

            if (isServer)
            {
                Entities
                    .WithoutBurst()
                    .ForEach((Entity entity, PurchaseRequest purchaseRequest) =>
                    {
                        if(PurchaseRequest.IsResultStatus(purchaseRequest.Status))
                        {
                            commands.DestroyEntity(0, entity);
                            
                            if (SystemAPI.HasComponent<InventoryTransaction>(purchaseRequest
                                    .InventoryTransactionEntity))
                            {
                                commands.DestroyEntity(0, purchaseRequest.InventoryTransactionEntity);
                            }
                        
                            if(SystemAPI.HasComponent<PlayerController>(purchaseRequest.Customer) == false)
                            {
                                return;
                            }
                        
                            var pc = SystemAPI.GetComponent<PlayerController>(purchaseRequest.Customer);
                            if(SystemAPI.HasComponent<TzarGames.MultiplayerKit.NetworkPlayer>(pc.Value) == false)
                            {
                                return;
                            }
                        
                            var player = SystemAPI.GetComponent<TzarGames.MultiplayerKit.NetworkPlayer>(pc.Value);
                            this.RPC(PurchaseResultRPC, player, purchaseRequest.Status, purchaseRequest.Guid);    
                        }

                    }).Run();
            }
        }

        public async System.Threading.Tasks.Task<PurchaseRequestStatus> RequestPuchase(Entity customer, Entity store, NativeArray<PurchaseRequest_Item> itemsToPurchase)
        {
            var storeNetId = SystemAPI.GetComponent<NetworkID>(store);
            var requestGuid = System.Guid.NewGuid();

            if (NetIdentity != null && NetIdentity.Net != null && NetIdentity.Net.IsServer == false)
            {
                this.RPCWithNativeArray(PurchaseItemRPC, itemsToPurchase, storeNetId, requestGuid);
            }
            else
            {
                PurchaseItemRPC(itemsToPurchase, storeNetId, requestGuid, new NetMessageInfo { SenderEntity = customer });
            }

            var time = UnityEngine.Time.realtimeSinceStartup;
            Entity resultEntity = Entity.Null;
            PurchaseRequestStatus result = PurchaseRequestStatus.UnknownError;

            while (time - UnityEngine.Time.realtimeSinceStartup < 10)
            {
                await System.Threading.Tasks.Task.Yield();
                bool gotResult = false;
                
                Entities
                    .WithStructuralChanges()
                    .WithoutBurst()
                    .ForEach((Entity entity, in PurchaseRequest request) =>
                {
                    if(PurchaseRequest.IsResultStatus(request.Status) == false)
                    {
                        return;
                    }

                    EntityManager.DestroyEntity(entity);

                    if (request.Guid != requestGuid)
                    {
                        Debug.LogError($"Request {request.Guid} not handled, status {request.Status}, guid {request.Guid}");
                        return;
                    }

                    gotResult = true;
                    result = request.Status;

                }).Run();

                if(gotResult)
                {
                    break;
                }
            }

            return result;
        }

        [RemoteCall]
        public void PurchaseResultRPC(PurchaseRequestStatus requestResult, System.Guid requestGuid)
        {
            Debug.Log($"PurchaseResultRPC {requestResult}");
            createPuchaseRequest(default, default, default, requestGuid, requestResult);
        }

        [RemoteCall(canBeCalledFromClient: true, canBeCalledByNonOwner: true)]
        public void PurchaseItemRPC(NativeArray<PurchaseRequest_Item> itemsToPurchase, NetworkID storeNetId, System.Guid requestGuid, NetMessageInfo netMessageInfo)
        {
#if UNITY_EDITOR
            Debug.Log($"PurchaseItemRPC storeNetID {storeNetId.ID} guid {requestGuid}, sender {netMessageInfo.Sender.ID}");
#endif

            if (itemsToPurchase.IsCreated == false || itemsToPurchase.Length == 0 || itemsToPurchase.Length > 100)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} - invalid number of items to purchase");
                sendPurchaseResult(PurchaseRequestStatus.UnknownError, requestGuid);
                return;
            }

            // проверяем, нет ли действующих запросов
            bool alreadyHasRequest = false;

            Entities
                .ForEach((in PurchaseRequest request) =>
            {
                if (request.Customer == netMessageInfo.SenderEntity)
                {
                    alreadyHasRequest = true;
                }
            }).Run();

            if (alreadyHasRequest)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} already requested a purchase");
                sendPurchaseResult(PurchaseRequestStatus.UnknownError, requestGuid);
                return;
            }

            if(SystemAPI.HasComponent<ControlledCharacter>(netMessageInfo.SenderEntity) == false)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} does not have {nameof(ControlledCharacter)} component");
                sendPurchaseResult(PurchaseRequestStatus.UnknownError, requestGuid);
                return;
            }
            var character = SystemAPI.GetComponent<ControlledCharacter>(netMessageInfo.SenderEntity);

            var netIdToEntity = new EntityFromNetworkId(storeNetIdsQuery, GetComponentTypeHandle<NetworkID>(), GetEntityTypeHandle(), Allocator.Temp);

            if(netIdToEntity.TryGet(storeNetId, out Entity store) == false)
            {
                Debug.LogError($"Failed to find store with netID {storeNetId.ID}");
                return;
            }
            
            createPuchaseRequest(character.Entity, store, itemsToPurchase, requestGuid, PurchaseRequestStatus.InProcess);
        }

        void sendPurchaseResult(PurchaseRequestStatus result, System.Guid requestGuid)
        {
            if (NetIdentity != null)
            {
                this.RPC(PurchaseResultRPC, result, requestGuid);
            }
            else
            {
                PurchaseResultRPC(result, requestGuid);
            }
        }

        Entity createPuchaseRequest(Entity customer, Entity store, NativeArray<PurchaseRequest_Item> itemsToPurchase, System.Guid requestGuid, PurchaseRequestStatus status)
        {
            var purchaseRequest = new PurchaseRequest
            {
                Guid = requestGuid,
                Customer = customer,
                Store = store,
                Status = status
            };

            var requestEntity = EntityManager.CreateEntity(typeof(PurchaseRequest), typeof(DestroyTimer), typeof(PurchaseRequest_Item));
            EntityManager.SetComponentData(requestEntity, purchaseRequest);
            EntityManager.SetComponentData(requestEntity, new DestroyTimer(5));

            if (itemsToPurchase.IsCreated && itemsToPurchase.Length > 0)
            {
                var items = EntityManager.GetBuffer<PurchaseRequest_Item>(requestEntity);
                items.AddRange(itemsToPurchase);    
            }

            return requestEntity;
        }

        void processPurchaseRequests(UniversalCommandBuffer commands)
        {
            var storeFromEntity = GetBufferLookup<StoreItems>(true);
            var objectDatabaseEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var databaseItems = SystemAPI.GetBuffer<IdToEntity>(objectDatabaseEntity);
            var inventories = GetBufferLookup<InventoryElement>(true);
            var invEventArchetype = inventoryEventArchetype;
            
            Entities.ForEach((Entity requestEntity, int entityInQueryIndex, DynamicBuffer<PurchaseRequest_Item> itemsToPurchase, ref PurchaseRequest request) =>
            {
                if (request.Status == PurchaseRequestStatus.InventoryValidation)
                {
                    if (SystemAPI.HasComponent<InventoryTransaction>(request.InventoryTransactionEntity) == false)
                    {
                        request.Status = PurchaseRequestStatus.InventoryError;
                        return;
                    }

                    var transaction = SystemAPI.GetComponent<InventoryTransaction>(request.InventoryTransactionEntity);

                    if (transaction.Status == InventoryTransactionStatus.Success)
                    {
                        request.Status = PurchaseRequestStatus.Success;
                    }
                    return;
                }
                
                if (request.Status != PurchaseRequestStatus.InProcess)
                {
                    return;
                }

                if (inventories.TryGetBuffer(request.Customer, out DynamicBuffer<InventoryElement> inventory) == false)
                {
                    Debug.LogError($"Покупатель {request.Customer.Index} не имеет инвентаря");
                    request.Status = PurchaseRequestStatus.UnknownError;
                    return;
                }

                if (storeFromEntity.TryGetBuffer(request.Store, out DynamicBuffer<StoreItems> storeItems) == false)
                {
                    Debug.LogError($"{request.Store} нет компонента ItemsStore");
                    request.Status = PurchaseRequestStatus.UnknownError;
                    return;
                }
                
                var mainCurrencyItem = Entity.Null;
                for (var i = 0; i < inventory.Length; i++)
                {
                    var inventoryItem = inventory[i];
                    if (SystemAPI.HasComponent<MainCurrency>(inventoryItem.Entity))
                    {
                        mainCurrencyItem = inventoryItem.Entity;
                        break;
                    }
                }

                if (mainCurrencyItem == Entity.Null)
                {
                    request.Status = PurchaseRequestStatus.NotEnoughMoney;
                    return;
                }
                
                long totalPrice = 0;

                var itemToPurchasePrefabs =
                    new NativeArray<Entity>(itemsToPurchase.Length, Allocator.Temp);

                for (int it=0; it<itemsToPurchase.Length; it++)
                {
                    var item = itemsToPurchase[it];
                    
                    if (item.Count <= 0)
                    {
                        Debug.LogError($"Неверное количество покупаемых предметов {item.Count}");
                        request.Status = PurchaseRequestStatus.UnknownError;
                        return;
                    }
                    
                    bool hasItem = false;

                    for (var i = 0; i < storeItems.Length; i++)
                    {
                        var storeItem = storeItems[i];
                        if (storeItem.ItemID == item.ItemID)
                        {
                            hasItem = true;
                            break;
                        }
                    }
                    
                    if (hasItem == false)
                    {
                        Debug.LogError($"В магазине {request.Store} нет предмета с ID {item.ItemID}");
                        request.Status = PurchaseRequestStatus.UnknownError;
                        return;
                    }
                    
                    var itemPrefab = IdToEntity.GetEntityByID(databaseItems, item.ItemID);

                    if (itemPrefab == Entity.Null)
                    {
                        Debug.LogError($"Не удалось найти предмет с ID {item.ItemID} в базе данных");
                        request.Status = PurchaseRequestStatus.UnknownError;
                        return;
                    }
                    
                    if (SystemAPI.HasComponent<Price>(itemPrefab) == false)
                    {
                        Debug.LogError($"Предмет {item.ItemID} не имеет компонента цены");
                        request.Status = PurchaseRequestStatus.UnknownError;
                        return;
                    }
                    
                    var price = SystemAPI.GetComponent<Price>(itemPrefab);
                    totalPrice += (price.Value * item.Count);

                    itemToPurchasePrefabs[it] = itemPrefab;
                }
                
                var currentCash = SystemAPI.GetComponent<Consumable>(mainCurrencyItem);

                if (totalPrice > currentCash.Count)
                {
                    request.Status = PurchaseRequestStatus.NotEnoughMoney;
                    return;
                }
                
                // inventory transaction
                var invRequestEntity = commands.CreateEntity(entityInQueryIndex, invEventArchetype);
                commands.SetComponent(entityInQueryIndex, invRequestEntity, new Target { Value = request.Customer });
                var toAdd = commands.SetBuffer<ItemsToAdd>(entityInQueryIndex, invRequestEntity);

                for (var it = 0; it < itemToPurchasePrefabs.Length; it++)
                {
                    var itemToPurchasePrefab = itemToPurchasePrefabs[it];
                    if (SystemAPI.HasComponent<Consumable>(itemToPurchasePrefab))
                    {
                        var instance = commands.Instantiate(entityInQueryIndex, itemToPurchasePrefab);
                        if (SystemAPI.HasComponent<SyncedColor>(itemToPurchasePrefab))
                        {
                            commands.SetComponent(entityInQueryIndex, instance, new SyncedColor(itemsToPurchase[it].Color));
                        }
                        commands.SetComponent(entityInQueryIndex, instance, new Consumable { Count = (uint)itemsToPurchase[it].Count });
                        toAdd.Add(new ItemsToAdd { Item = instance });
                    }
                    else
                    {
                        for (int c = 0; c < itemsToPurchase[it].Count; c++)
                        {
                            var instance = commands.Instantiate(entityInQueryIndex, itemToPurchasePrefab);
                            if (SystemAPI.HasComponent<SyncedColor>(itemToPurchasePrefab))
                            {
                                commands.SetComponent(entityInQueryIndex, instance, new SyncedColor(itemsToPurchase[it].Color));
                            }
                            toAdd.Add(new ItemsToAdd { Item = instance });
                        }
                    }
                }

                var toRemove = commands.SetBuffer<ItemsToRemove>(entityInQueryIndex, invRequestEntity);
                toRemove.Add(new ItemsToRemove(mainCurrencyItem, (uint)totalPrice));
                
                // waiting for inventory transaction validation
                request.Status = PurchaseRequestStatus.InventoryValidation;
                request.InventoryTransactionEntity = invRequestEntity;
                
                commands.SetComponent(entityInQueryIndex, requestEntity, request);
            })
            .Run();
        }
    }
}
