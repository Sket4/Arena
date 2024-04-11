using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(StatefulTriggerEventSystem))]
    [UpdateBefore(typeof(StoreSystem))]
    public partial class FilterPurchaseRequestSystem : SystemBase
    {
        private EntityQuery requestsQuery;
        private EntityQuery matchStateQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            requestsQuery = GetEntityQuery(typeof(PurchaseRequest));
            matchStateQuery = GetEntityQuery(ComponentType.ReadOnly<ArenaMatchStateData>());
        }

        protected override void OnUpdate()
        {
            if (requestsQuery.CalculateEntityCount() == 0)
            {
                return;
            }

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
            }
            
            var overlappingBuffers = GetBufferLookup<OverlappingEntities>(true);
            
            Entities
                .WithReadOnly(overlappingBuffers)
                .ForEach((ref PurchaseRequest request) =>
            {
                if (request.Status != PurchaseRequestStatus.InProcess)
                {
                    return;
                }
                if (SystemAPI.HasComponent<InteractiveObject>(request.Store) == false)
                {
                    return;
                }

                if (overlappingBuffers.HasBuffer(request.Customer) == false)
                {
                    return;
                }

                var overlappings = overlappingBuffers[request.Customer];
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
                
            }).Run();
        }
    }
}
