using System;
using Arena.Items;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using NetworkPlayer = TzarGames.MultiplayerKit.NetworkPlayer;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(InventorySystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class StoreSystem : GameSystemBase, IRpcProcessor
    {
        private EntityQuery storeNetIdsQuery;
        public EntityArchetype inventoryEventArchetype;
        private BufferLookup<StoreItems> storeFromEntity;
        private BufferLookup<InventoryElement> inventories;
        private ComponentLookup<Item> itemLookup;
        private ComponentLookup<Price> priceLookup;
        private ComponentLookup<InventoryTransaction> transactionLookup;
        private ComponentLookup<PlayerController> pcLookup;
        private Entity mainCurrencyPrefab;
        public NetworkIdentity NetIdentity { get; set; }
        private SellRequestPostprocessJob sellRequestPostprocessJob; 

        protected override void OnCreate()
        {
            base.OnCreate();
            
            inventoryEventArchetype = EntityManager.CreateArchetype(
                typeof(InventoryTransaction), 
                typeof(Target),
                typeof(ItemsToAdd), 
                typeof(ItemsToRemove));
            
            storeNetIdsQuery = GetEntityQuery(ComponentType.ReadOnly<StoreItems>(), ComponentType.ReadOnly<NetworkID>());
            
            storeFromEntity = GetBufferLookup<StoreItems>(true);
            inventories = GetBufferLookup<InventoryElement>(true);
            itemLookup = GetComponentLookup<Item>(true);
            priceLookup = GetComponentLookup<Price>(true);
            transactionLookup = GetComponentLookup<InventoryTransaction>(true);
            pcLookup = GetComponentLookup<PlayerController>(true);
            
            sellRequestPostprocessJob.Caller.InitializeMethod<SellRequestStatus,System.Guid>(SellResultRPC, out sellRequestPostprocessJob.SellResultRPC);
            
            RequireForUpdate<MainDatabaseTag>();
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
            storeFromEntity.Update(this);
            itemLookup.Update(this);
            priceLookup.Update(this);
            inventories.Update(this);
            transactionLookup.Update(this);
            
            var dbEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var databaseItems = SystemAPI.GetBuffer<IdToEntity>(dbEntity).AsNativeArray();

            if (mainCurrencyPrefab == Entity.Null)
            {
                foreach (var databaseItem in databaseItems)
                {
                    if (EntityManager.HasComponent<MainCurrency>(databaseItem.Entity))
                    {
                        mainCurrencyPrefab = databaseItem.Entity;
                        break;
                    }
                }

                if (mainCurrencyPrefab == Entity.Null)
                {
                    Debug.LogError("Failed to find main currency prefab");
                    return;
                }
            }
            
            processPurchaseRequests(commands, databaseItems);
            processSellRequests(commands, databaseItems);

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
            
                var parallelCommands = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
                sellRequestPostprocessJob.Caller.InitializeCaller(parallelCommands, NetIdentity.RpcSystem);
                sellRequestPostprocessJob.SystemNetworkID = NetIdentity.ID;
                sellRequestPostprocessJob.TransactionLookup = transactionLookup;
                sellRequestPostprocessJob.PlayerControllerLookup = pcLookup;
                sellRequestPostprocessJob.Run();
            }
        }

        [BurstCompile]
        partial struct SellRequestPostprocessJob : IJobEntity, IParallelJobRpcCaller
        {
            [ReadOnly] public ComponentLookup<InventoryTransaction> TransactionLookup;
            [ReadOnly] public ComponentLookup<PlayerController> PlayerControllerLookup;
            
            public SimpleParallelJobRpcCaller Caller;
            public RemoteCallInfo SellResultRPC;
            public EntityArchetype RpcArchetype { get; set; }
            public EntityCommandBuffer.ParallelWriter CommandBuffer { get; set; }
            public NetworkID SystemNetworkID;
            
            public void Execute(Entity entity, [EntityIndexInChunk] int cmdIndex, SellRequest sellRequest)
            {
                if (SellRequest.IsResultStatus(sellRequest.Status) == false)
                {
                    return;
                }
                
                CommandBuffer.DestroyEntity(cmdIndex, entity);
                            
                if (TransactionLookup.HasComponent(sellRequest.InventoryTransactionEntity))
                {
                    CommandBuffer.DestroyEntity(cmdIndex, sellRequest.InventoryTransactionEntity);
                }
                        
                if(PlayerControllerLookup.TryGetComponent(sellRequest.Seller, out var pc) == false)
                {
                    return;
                }
                IParallelJobRpcCallerExtensions.RPC(Caller, cmdIndex, SellResultRPC, SystemNetworkID, sellRequest.Status, sellRequest.Guid);
            }
        }

        public async System.Threading.Tasks.Task<PurchaseRequestStatus> RequestPurchase(Entity customer, Entity store, NativeArray<PurchaseRequest_Item> itemsToPurchase)
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
            var result = PurchaseRequestStatus.UnknownError;

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

        public async System.Threading.Tasks.Task<SellRequestStatus> RequestSell(Entity seller, Entity store, NativeArray<SellRequest_Item> itemsToSell)
        {
            if (itemsToSell.Length == 0)
            {
                return SellRequestStatus.InvalidRequest;
            }
            
            var storeNetId = SystemAPI.GetComponent<NetworkID>(store);
            var requestGuid = System.Guid.NewGuid();

            if (NetIdentity != null && NetIdentity.Net != null && NetIdentity.Net.IsServer == false)
            {
                var sellItemList = new NativeList<SellRequest_NetItem>(itemsToSell.Length, Allocator.Temp);
                foreach (var item in itemsToSell)
                {
                    sellItemList.Add(new SellRequest_NetItem
                    {
                        Count = item.Count,
                        ID = EntityManager.GetComponentData<NetworkID>(item.ItemEntity),
                    });
                }
                this.RPCWithNativeArray(SellItemRPC, sellItemList.AsArray(), storeNetId, requestGuid);
            }
            else
            {
                createSellRequest(seller, store, itemsToSell, requestGuid, SellRequestStatus.InProcess);
            }

            var time = UnityEngine.Time.realtimeSinceStartup;
            var result = SellRequestStatus.UnknownError;

            while (time - UnityEngine.Time.realtimeSinceStartup < 10)
            {
                await System.Threading.Tasks.Task.Yield();
                bool gotResult = false;
                
                Entities
                    .WithStructuralChanges()
                    .WithoutBurst()
                    .ForEach((Entity entity, in SellRequest request) =>
                {
                    if(SellRequest.IsResultStatus(request.Status) == false)
                    {
                        return;
                    }

                    EntityManager.DestroyEntity(entity);

                    if (request.Guid != requestGuid)
                    {
                        Debug.LogError($"Sell request {request.Guid} not handled, status {request.Status}, guid {request.Guid}");
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
        public void SellResultRPC(SellRequestStatus result, System.Guid requestGuid)
        {
            Debug.Log($"SellResultRPC {result}");
            createSellRequest(default, default, default, requestGuid, result);
        }
        Entity createSellRequest(Entity seller, Entity store, NativeArray<SellRequest_Item> itemsToSell, System.Guid requestGuid, SellRequestStatus status)
        {
            var request = new SellRequest
            {
                Guid = requestGuid,
                Seller = seller,
                Store = store,
                Status = status
            };

            var requestEntity = EntityManager.CreateEntity(typeof(SellRequest), typeof(SellRequest_Item));
            EntityManager.SetComponentData(requestEntity, request);

            if (itemsToSell.IsCreated && itemsToSell.Length > 0)
            {
                var items = EntityManager.GetBuffer<SellRequest_Item>(requestEntity);
                items.AddRange(itemsToSell);
            }

            return requestEntity;
        }
        void sendSellResult(SellRequestStatus result, System.Guid requestGuid)
        {
            if (NetIdentity != null)
            {
                this.RPC(SellResultRPC, result, requestGuid);
            }
            else
            {
                SellResultRPC(result, requestGuid);
            }
        }
        
        [RemoteCall(canBeCalledFromClient: true, canBeCalledByNonOwner: true)]
        public void SellItemRPC(NativeArray<SellRequest_NetItem> itemsNetIdsToSell, NetworkID storeNetId, System.Guid requestGuid, NetMessageInfo netMessageInfo)
        {
#if UNITY_EDITOR
            Debug.Log($"SellItemRPC storeNetID {storeNetId.ID} guid {requestGuid}, sender {netMessageInfo.Sender.ID}");
#endif

            if (itemsNetIdsToSell.IsCreated == false || itemsNetIdsToSell.Length == 0 || itemsNetIdsToSell.Length > 100)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} - invalid number of items to sell");
                sendSellResult(SellRequestStatus.InvalidRequest, requestGuid);
                return;
            }

            // проверяем, нет ли действующих запросов
            bool alreadyHasRequest = false;

            Entities
                .ForEach((in SellRequest request) =>
            {
                if (request.Seller == netMessageInfo.SenderEntity)
                {
                    alreadyHasRequest = true;
                }
            }).Run();

            if (alreadyHasRequest)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} already requested a sell");
                sendSellResult(SellRequestStatus.SellAlreadyInProcess, requestGuid);
                return;
            }

            if(SystemAPI.HasComponent<ControlledCharacter>(netMessageInfo.SenderEntity) == false)
            {
                Debug.LogError($"Player {netMessageInfo.Sender.ID} with entity {netMessageInfo.SenderEntity.Index} does not have {nameof(ControlledCharacter)} component");
                sendSellResult(SellRequestStatus.InvalidCharacter, requestGuid);
                return;
            }
            var character = SystemAPI.GetComponent<ControlledCharacter>(netMessageInfo.SenderEntity);

            var netIdToEntity = new EntityFromNetworkId(storeNetIdsQuery, GetComponentTypeHandle<NetworkID>(), GetEntityTypeHandle(), Allocator.Temp);

            if(netIdToEntity.TryGet(storeNetId, out Entity store) == false)
            {
                Debug.LogError($"Failed to find store with netID {storeNetId.ID}");
                return;
            }

            var itemsToSell = new NativeList<SellRequest_Item>(itemsNetIdsToSell.Length, Allocator.Temp);

            foreach (var itemNetId in itemsNetIdsToSell)
            {
                if (netIdToEntity.TryGet(itemNetId.ID, out var itemEntity) == false)
                {
                    Debug.LogError($"Failed to find item entity by its ID: {itemNetId}");
                    return;
                }

                itemsToSell.Add(new SellRequest_Item
                {
                    Count = itemNetId.Count,
                    ItemEntity = itemEntity
                });
            }
            
            createSellRequest(character.Entity, store, itemsToSell, requestGuid, SellRequestStatus.InProcess);
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

            var requestEntity = EntityManager.CreateEntity(typeof(PurchaseRequest), typeof(PurchaseRequest_Item));
            EntityManager.SetComponentData(requestEntity, purchaseRequest);

            if (itemsToPurchase.IsCreated && itemsToPurchase.Length > 0)
            {
                var items = EntityManager.GetBuffer<PurchaseRequest_Item>(requestEntity);
                items.AddRange(itemsToPurchase);    
            }

            return requestEntity;
        }

        void processSellRequests(UniversalCommandBuffer commands, NativeArray<IdToEntity> prefabDatabase)
        {
            var sellRequestJob = new SellRequestJob
            {
                InventoryEventArchetype = inventoryEventArchetype,
                InventoryLookup = inventories,
                StoreItemsLookup = storeFromEntity,
                Commands = commands,
                PriceLookup = priceLookup,
                MainCurrencyPrefab = mainCurrencyPrefab,
                InventoryTransactionLookup = transactionLookup,
            };
            sellRequestJob.Run();
        }

        [BurstCompile]
        partial struct SellRequestJob : IJobEntity
        {
            public EntityArchetype InventoryEventArchetype;
            public Entity MainCurrencyPrefab;
            [ReadOnly] public ComponentLookup<InventoryTransaction> InventoryTransactionLookup;
            [ReadOnly] public BufferLookup<InventoryElement> InventoryLookup;
            [ReadOnly] public BufferLookup<StoreItems> StoreItemsLookup;
            [ReadOnly] public ComponentLookup<Price> PriceLookup;
            public UniversalCommandBuffer Commands;
            
            public void Execute(
                Entity requestEntity, 
                [EntityIndexInChunk] int cmdIndex,
                in DynamicBuffer<SellRequest_Item> itemsToSell, 
                ref SellRequest request)
            {
                if (request.Status == SellRequestStatus.InventoryValidation)
                {
                    if (InventoryTransactionLookup.TryGetComponent(request.InventoryTransactionEntity, out var transaction) == false)
                    {
                        request.Status = SellRequestStatus.InventoryError;
                        return;
                    }

                    switch (transaction.Status)
                    {
                        case InventoryTransactionStatus.Processing:
                            break;
                        case InventoryTransactionStatus.Failed:
                            request.Status = SellRequestStatus.InventoryError;
                            break;
                        case InventoryTransactionStatus.Success:
                            request.Status = SellRequestStatus.Success;
                            break;
                    }
                    return;
                }
                
                if (request.Status != SellRequestStatus.InProcess)
                {
                    return;
                }
                
                if (InventoryLookup.TryGetBuffer(request.Seller, out DynamicBuffer<InventoryElement> inventory) == false)
                {
                    Debug.LogError($"Покупатель {request.Seller.Index} не имеет инвентаря");
                    request.Status = SellRequestStatus.NoInventoryError;
                    return;
                }
                
                if (StoreItemsLookup.TryGetBuffer(request.Store, out DynamicBuffer<StoreItems> storeItems) == false)
                {
                    Debug.LogError($"{request.Store} нет компонента ItemsStore");
                    request.Status = SellRequestStatus.InvalidStore;
                    return;
                }
                
                long totalPrice = 0;

                for (int it=0; it<itemsToSell.Length; it++)
                {
                    var item = itemsToSell[it];
                    
                    if (item.Count <= 0)
                    {
                        Debug.LogError($"Неверное количество покупаемых предметов {item.Count}");
                        request.Status = SellRequestStatus.WrongItemRequest;
                        return;
                    }
                    
                    if (PriceLookup.TryGetComponent(item.ItemEntity, out var price) == false)
                    {
                        Debug.LogError($"Предмет {item.ItemEntity.Index} не имеет компонента цены");
                        request.Status = SellRequestStatus.InvalidItemPrice;
                        return;
                    }
                    
                    totalPrice += price.Value * item.Count;
                }

                totalPrice = GetSellPrice(totalPrice);

                if (totalPrice > uint.MaxValue)
                {
                    request.Status = SellRequestStatus.TotalPriceError;
                    return;
                }
                
                // inventory transaction
                var invRequestEntity = Commands.CreateEntity(cmdIndex, InventoryEventArchetype);
                Commands.SetComponent(cmdIndex, invRequestEntity, new Target { Value = request.Seller });
                var toRemove = Commands.SetBuffer<ItemsToRemove>(cmdIndex, invRequestEntity);

                int index = 0;
                foreach (var item in itemsToSell)
                {
                    toRemove.Add(new ItemsToRemove { Item = item.ItemEntity, Count = item.Count });
                }

                var toAdd = Commands.SetBuffer<ItemsToAdd>(cmdIndex, invRequestEntity);
                var mainCurrencyInstance = Commands.Instantiate(cmdIndex, MainCurrencyPrefab);
                Commands.SetComponent(cmdIndex, mainCurrencyInstance, new Consumable
                {
                    Count = (uint)totalPrice
                });
                
                toAdd.Add(new ItemsToAdd
                {
                    Item = mainCurrencyInstance 
                });
                
                // waiting for inventory transaction validation
                request.Status = SellRequestStatus.InventoryValidation;
                request.InventoryTransactionEntity = invRequestEntity;
                
                Commands.SetComponent(cmdIndex, requestEntity, request);
            }
        }

        void processPurchaseRequests(UniversalCommandBuffer commands, NativeArray<IdToEntity> prefabDatabase)
        {
            var invEventArchetype = inventoryEventArchetype;
            var inventoryLookup = inventories;
            var storeItemsLookup = storeFromEntity;
            var transactions = transactionLookup;
            
            Entities.ForEach((Entity requestEntity, int entityInQueryIndex, DynamicBuffer<PurchaseRequest_Item> itemsToPurchase, ref PurchaseRequest request) =>
            {
                if (request.Status == PurchaseRequestStatus.InventoryValidation)
                {
                    if (transactions.TryGetComponent(request.InventoryTransactionEntity, out var transaction) == false)
                    {
                        request.Status = PurchaseRequestStatus.InventoryError;
                        return;
                    }

                    switch (transaction.Status)
                    {
                        case InventoryTransactionStatus.Processing:
                            break;
                        case InventoryTransactionStatus.Failed:
                            request.Status = PurchaseRequestStatus.InventoryError;
                            break;
                        case InventoryTransactionStatus.Success:
                            request.Status = PurchaseRequestStatus.Success;
                            break;
                    }
                    return;
                }
                
                if (request.Status != PurchaseRequestStatus.InProcess)
                {
                    return;
                }

                if (inventoryLookup.TryGetBuffer(request.Customer, out DynamicBuffer<InventoryElement> inventory) == false)
                {
                    Debug.LogError($"Покупатель {request.Customer.Index} не имеет инвентаря");
                    request.Status = PurchaseRequestStatus.UnknownError;
                    return;
                }

                if (storeItemsLookup.TryGetBuffer(request.Store, out DynamicBuffer<StoreItems> storeItems) == false)
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
                    
                    var itemPrefab = IdToEntity.GetEntityByID(prefabDatabase, item.ItemID);

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

        public static long GetSellPrice(long itemPrice)
        {
            if (itemPrice > 1)
            {
                return itemPrice /= 2;    
            }
            return itemPrice;
        }
    }
}
