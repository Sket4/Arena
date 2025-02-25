using System.Threading.Tasks;
using Arena.Items;
using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using UniRx;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class ClientArenaMatchSystem : GameSystemBase, IRpcProcessor
    {
        public NetworkIdentity NetIdentity { get; set; }

        struct RpcDummy : IServerArenaCommands
        {
            public void NotifyExitingFromGame(bool requestMatchFinish, NetMessageInfo info) {}

            public void RequestContinueGame(NetMessageInfo info) {}

            public void RequestRestart(NetMessageInfo info) {}
        }

        RpcDummy rpc = new RpcDummy();
        private EntityQuery gameInterfaceQuery;
        private EntityQuery arenaMatchDataQuery;

        struct IsExitingFromGame : IComponentData
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            gameInterfaceQuery = GetEntityQuery(ComponentType.ReadOnly<GameInterface>());
            arenaMatchDataQuery = GetEntityQuery(ComponentType.ReadOnly<ArenaMatchStateData>());
        }

        protected override void OnSystemUpdate()
        {
        }

        public void RequestContinueGame(Entity player)
        {
            if (NetIdentity != null && NetIdentity.Net != null && NetIdentity.Net.IsConnected)
            {
                this.RPC(rpc.RequestContinueGame);
            }
            else
            {
                if (NetIdentity == null)
                {
                    World.GetExistingSystemManaged<Server.ArenaMatchSystem>().RequestContinueGame(new NetMessageInfo() { SenderEntity = player });    
                }
            }
        }

        public void NotifyExitFromGame(bool requestMatchFinish, Entity player, bool callGameInterface = true)
        {
            if (EntityManager.HasComponent<IsExitingFromGame>(player))
            {
                Debug.Log($"Player {player} already exiting from game");
                return;
            }
            Debug.Log($"Player {player} exiting from game");
            EntityManager.AddComponentData(player, new IsExitingFromGame());
            
            if (NetIdentity != null && NetIdentity.Net != null && NetIdentity.Net.IsConnected)
            {
                this.RPC(rpc.NotifyExitingFromGame, requestMatchFinish);
            }
            else
            {
                if (NetIdentity == null)
                {
                    World.GetExistingSystemManaged<Server.ArenaMatchSystem>().NotifyExitingFromGame(requestMatchFinish, new NetMessageInfo() { SenderEntity = player });    
                }
            }

            if (callGameInterface)
            {
                exitFromGame();
            }
        }

        async void exitFromGame()
        {
            var waitStartTime = UnityEngine.Time.realtimeSinceStartup;

            while (UnityEngine.Time.realtimeSinceStartup - waitStartTime < 3)
            {
                if (EntityManager.World.IsCreated == false)
                {
                    break;
                }

                if (arenaMatchDataQuery.HasSingleton<ArenaMatchStateData>() == false)
                {
                    break;
                }
                
                var state = arenaMatchDataQuery.GetSingleton<ArenaMatchStateData>();

                if (state.Saved)
                {
                    break;
                }
                
                await Task.Yield();
            }

            gameInterfaceQuery.TryGetSingleton(out GameInterface gameInterface);
            gameInterface.ExitFromMatch(); 
        }

#if UNITY_EDITOR
        [TzarGames.Common.ConsoleCommand]
        async void PurchaseItemFromStore(int itemID, int count)
        {
            Task<PurchaseRequestStatus> purchaseTask;
            
            using (var storeQuery = EntityManager.CreateEntityQuery(typeof(StoreItems)))
            using (var entities = storeQuery.ToEntityArray(Allocator.Temp))
            {
                var store = entities[0];
                
                Debug.Log($"Попытка покупки предмета {itemID} в магазине {store}");
                
                var playerEntity = GetLocalPlayerCharacterEntity();
                
                var storeSystem = EntityManager.World.GetExistingSystemManaged<StoreSystem>();
                var list = new NativeArray<PurchaseRequest_Item>(1, Allocator.Temp);
                list[0] = new PurchaseRequest_Item { ItemID = itemID, Count = count, Color = Color.white };
                purchaseTask = storeSystem.RequestPurchase(playerEntity, store, list);
            }

            var result = await purchaseTask;
            Debug.Log($"Результат покупки: {result}");
        }
        
        [ConsoleCommand]
        async void SellItemFromStore(int itemID, int count)
        {
            Task<SellRequestStatus> sellTask;
            
            using (var storeQuery = EntityManager.CreateEntityQuery(typeof(StoreItems)))
            using (var entities = storeQuery.ToEntityArray(Allocator.Temp))
            {
                var store = entities[0];
                
                Debug.Log($"Попытка покупки предмета {itemID} в магазине {store}");
                
                var playerEntity = GetLocalPlayerCharacterEntity();
                var inventory = EntityManager.GetBuffer<InventoryElement>(playerEntity);

                if (InventoryExtensions.TryGetItemEntityWithId(inventory, itemID, EntityManager, out var itemEntity) == false)
                {
                    Debug.LogError($"Не найден предмет {itemID} в инвентаре игрока");
                    return;
                }
                
                var storeSystem = EntityManager.World.GetExistingSystemManaged<StoreSystem>();
                var list = new NativeArray<SellRequest_Item>(1, Allocator.Temp);
                list[0] = new SellRequest_Item { ItemEntity = itemEntity, Count = (uint)count };
                sellTask = storeSystem.RequestSell(playerEntity, store, list);
            }

            var result = await sellTask;
            Debug.Log($"Результат продажи: {result}");
        }
        
        [ConsoleCommand]
        void CraftItem(int itemID)
        {
            using (var crafterQuery = EntityManager.CreateEntityQuery(typeof(CraftReceipts)))
            using (var entities = crafterQuery.ToEntityArray(Allocator.Temp))
            {
                var crafter = entities[0];
                var playerEntity = GetLocalPlayerCharacterEntity();
                var receipts = EntityManager.GetBuffer<CraftReceipts>(crafter);
                var receiptToCraft = Entity.Null;

                foreach (var receipt in receipts)
                {
                    var receiptData = EntityManager.GetComponentData<CraftReceipt>(receipt.Receipt);
                    if (EntityManager.GetComponentData<Item>(receiptData.Item).ID == itemID)
                    {
                        receiptToCraft = receipt.Receipt;
                        break;
                    }
                }

                if (receiptToCraft == Entity.Null)
                {
                    Debug.LogError($"Не найден рецепт для предмета {itemID} у крафтера {crafter}");
                    return;
                }
                
                var request = new CraftRequest
                {
                    Crafter = crafter,
                    Receipt = receiptToCraft,
                    InventoryOwner = playerEntity
                };

                var requestEntity = EntityManager.CreateEntity(typeof(CraftRequest));
                EntityManager.SetComponentData(requestEntity, request);
                
                Debug.Log($"Попытка крафта предмета {itemID} у крафтера {crafter}");
                
                UniRx.Observable.EveryLateUpdate()
                    .TakeWhile(x => EntityManager.Exists(requestEntity))
                    .DelayFrame(1)
                    .Subscribe(_ =>
                    {
                        var result = EntityManager.GetComponentData<CraftRequest>(requestEntity);
                        Debug.Log($"Статус крафта: {result.State}");
                        
                        if (result.State == CraftReceiptState.Pending || result.State == CraftReceiptState.Processing)
                        {
                            return;
                        }
                        
                        EntityManager.DestroyEntity(requestEntity);
                    });

            }
        }

        [TzarGames.Common.ConsoleCommand]
        void AddMoney(int count)
        {
            var characterEntity = GetLocalPlayerCharacterEntity();
            var currencyPrefab = GetEntityWithComponent<MainCurrency>(true);
            var currencyEntity = EntityManager.Instantiate(currencyPrefab);
            EntityManager.SetComponentData(currencyEntity, new Consumable { Count = (uint)count });
            var requestEntity = EntityManager.CreateEntity(typeof(InventoryTransaction), typeof(ItemsToAdd), typeof(Target));

            EntityManager.SetComponentData(requestEntity, new Target { Value = characterEntity });
            var toAdd = EntityManager.GetBuffer<ItemsToAdd>(requestEntity);
            toAdd.Add(new ItemsToAdd { Item = currencyEntity });

            Debug.Log($"Запрос на добавление {count} валюты");
        }

        [TzarGames.Common.ConsoleCommand]
        void SelfDamage()
        {
            var characterEntity = GetLocalPlayerCharacterEntity();
            var entity = EntityManager.CreateEntity(typeof(EventTag), typeof(Target), typeof(ModifyHealth));
            EntityManager.SetComponentData(entity, new Target { Value = characterEntity });
            EntityManager.SetComponentData(entity, new ModifyHealth { Mode = ModifyHealthMode.AddPercent, Value = -33 });
        }

        public Entity GetEntityWithComponent<T>(bool prefab)
        {
            using (var query = (prefab ?  EntityManager.CreateEntityQuery(typeof(T), typeof(Prefab)) : EntityManager.CreateEntityQuery(typeof(T))))
            using(var entities = query.ToEntityArray(Allocator.Temp))    
            {
                if (entities.Length > 0)
                {
                    return entities[0];
                }
            }
            return Entity.Null;
        }
        
        [TzarGames.Common.ConsoleCommand]
        void AddXpToLocalPlayer(int xp)
        {
            var characterEntity = GetLocalPlayerCharacterEntity();

            var xpEvent = EntityManager.CreateEntity();

            var addXp = new AddXP()
            {
                Target = characterEntity,
                Value = xp
            };
            EntityManager.AddComponentData(xpEvent, new EventTag());
            EntityManager.AddComponentData(xpEvent, addXp);
        }
        
        [TzarGames.Common.ConsoleCommand]
        void GodMode(bool enable)
        {
            var characterEntity = GetLocalPlayerCharacterEntity();

            var defenseMods = EntityManager.GetBuffer<DefenseModificator>(characterEntity);

            if(enable)
            {
                defenseMods.Add(new DefenseModificator { Owner = characterEntity, Value = new CharacteristicModificator { Operator = ModificatorOperators.MULTIPLY_ACTUAL, Value = 999999 } });
                Debug.Log("Enabled god mode from local player character");
            }
            else
            {
                IOwnedModificatorExtensions.RemoveModificatorWithOwner(characterEntity, defenseMods);
                Debug.Log("Removed god mode from local player character");
            }
        }

        [TzarGames.Common.ConsoleCommand]
        void EnableDamageForPlayer(bool enable)
        {
            var characterEntity = GetLocalPlayerCharacterEntity();

            var damageModificators = EntityManager.GetBuffer<DamageModificator>(characterEntity);

            if(enable)
            {
                IOwnedModificatorExtensions.RemoveModificatorWithOwner(characterEntity, damageModificators);
            }
            else
            {
                damageModificators.Add(new DamageModificator { Owner = characterEntity, Value = new CharacteristicModificator { Operator = ModificatorOperators.MULTIPLY_ACTUAL, Value = 0 } });
            }
        }

        Entity GetLocalPlayerEntity()
        {
            using (var playerQuery = EntityManager.CreateEntityQuery(typeof(Player)))
            using (var players = playerQuery.ToComponentDataArray<Player>(Unity.Collections.Allocator.TempJob))
            using (var playerEntities = playerQuery.ToEntityArray(Unity.Collections.Allocator.TempJob))
            {
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];

                    if (player.ItsMe == false)
                    {
                        continue;
                    }

                    return playerEntities[i];
                }
                return Entity.Null;
            }
        }

        Entity GetLocalPlayerCharacterEntity()
        {
            var player = GetLocalPlayerEntity();

            if (player == Entity.Null)
            {
                return Entity.Null;
            }

            var character = EntityManager.GetComponentData<ControlledCharacter>(player);
            return character.Entity;
        }
#endif
    }
}
