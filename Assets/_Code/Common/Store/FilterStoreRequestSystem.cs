using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(StatefulTriggerEventSystem))]
    [UpdateBefore(typeof(StoreSystem))]
    public partial class FilterStoreRequestSystem : SystemBase
    {
        private EntityQuery matchStateQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            matchStateQuery = GetEntityQuery(ComponentType.ReadOnly<ArenaMatchStateData>());
            RequireForUpdate<MainDatabaseTag>();
        }

        protected override void OnUpdate()
        {
            if(matchStateQuery.CalculateEntityCount() > 0)
            {
                var arenaState = matchStateQuery.GetSingleton<ArenaMatchStateData>();
                
                Entities.ForEach((ref PurchaseRequest request) =>
                {
                    if (request.Status != PurchaseRequestStatus.InProcess)
                    {
                        return;
                    }

                    if (arenaState.State != ArenaMatchState.Preparing)
                    {
                        request.Status = PurchaseRequestStatus.StoreUnavailable;
                    }
                }).Run();
                
                Entities.ForEach((ref SellRequest request) =>
                {
                    if (request.Status != SellRequestStatus.InProcess)
                    {
                        return;
                    }

                    if (arenaState.State != ArenaMatchState.Preparing)
                    {
                        request.Status = SellRequestStatus.StoreUnavailable;
                    }
                }).Run();
            }
            
            var overlappingBuffers = GetBufferLookup<OverlappingEntities>(true);
            var mainDatabaseEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
            var mainDatabase = SystemAPI.GetBuffer<IdToEntity>(mainDatabaseEntity).AsNativeArray();
            
            Entities
                .WithReadOnly(overlappingBuffers)
                .WithReadOnly(mainDatabase)
                .ForEach((DynamicBuffer<PurchaseRequest_Item> items, ref PurchaseRequest request) =>
            {
                if (request.Status != PurchaseRequestStatus.InProcess)
                {
                    return;
                }
                if (SystemAPI.HasComponent<InteractiveObject>(request.Store) == false)
                {
                    request.Status = PurchaseRequestStatus.StoreUnavailable;
                    return;
                }

                if (SystemAPI.HasBuffer<LinkedEntityGroup>(request.Customer) == false)
                {
                    request.Status = PurchaseRequestStatus.CustomerCheckError;
                    return;
                }

                var linkeds = SystemAPI.GetBuffer<LinkedEntityGroup>(request.Customer);

                DynamicBuffer<OverlappingEntities> overlappings = default;
                    
                foreach (var linked in linkeds)
                {
                    if (overlappingBuffers.TryGetBuffer(linked.Value, out overlappings))
                    {
                        break;
                    }
                }

                if (overlappings.IsCreated == false)
                {
                    request.Status = PurchaseRequestStatus.NoCharacterContact;
                    return;
                }

                bool isInteracting = false;
                
                foreach (var overlapping in overlappings)
                {
                    if (overlapping.Entity == request.Store)
                    {
                        isInteracting = true;
                        break;
                    }
                }

                if (isInteracting == false)
                {
                    request.Status = PurchaseRequestStatus.StoreUnavailable;
                }

                var customerGender = SystemAPI.GetComponent<Gender>(request.Customer).Value;

                foreach (var item in items)
                {
                    if (IdToEntity.TryGetEntityById(mainDatabase, item.ItemID, out var itemPrefab) == false)
                    {
                        request.Status = PurchaseRequestStatus.ItemPrefabError;
                        break;
                    }

                    if (SystemAPI.HasComponent<Gender>(itemPrefab))
                    {
                        var itemGender = SystemAPI.GetComponent<Gender>(itemPrefab).Value;

                        if (itemGender != customerGender)
                        {
                            request.Status = PurchaseRequestStatus.GenderError;
                            break;
                        }
                    }
                }

            }).Run();
            
            Entities
                .WithReadOnly(overlappingBuffers)
                .ForEach((ref SellRequest request) =>
                {
                    if (request.Status != SellRequestStatus.InProcess)
                    {
                        return;
                    }
                    if (SystemAPI.HasComponent<InteractiveObject>(request.Store) == false)
                    {
                        request.Status = SellRequestStatus.StoreUnavailable;
                        return;
                    }

                    if (SystemAPI.HasBuffer<LinkedEntityGroup>(request.Seller) == false)
                    {
                        request.Status = SellRequestStatus.SellerCheckError;
                        return;
                    }

                    var linkeds = SystemAPI.GetBuffer<LinkedEntityGroup>(request.Seller);

                    DynamicBuffer<OverlappingEntities> overlappings = default;
                    
                    foreach (var linked in linkeds)
                    {
                        if (overlappingBuffers.TryGetBuffer(linked.Value, out overlappings))
                        {
                            break;
                        }
                    }

                    if (overlappings.IsCreated == false)
                    {
                        request.Status = SellRequestStatus.NoCharacterContact;
                        return;
                    }

                    bool isInteracting = false;
                
                    foreach (var overlapping in overlappings)
                    {
                        if (overlapping.Entity == request.Store)
                        {
                            isInteracting = true;
                            break;
                        }
                    }

                    if (isInteracting == false)
                    {
                        request.Status = SellRequestStatus.StoreUnavailable;
                    }
                
                }).Run();
        }
    }
}
