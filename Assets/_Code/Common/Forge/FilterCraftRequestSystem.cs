using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(StatefulTriggerEventSystem))]
    [UpdateBefore(typeof(CraftSystem))]
    public partial class FilterCraftRequestSystem : SystemBase
    {
        private EntityQuery requestsQuery;
        private EntityQuery matchStateQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            requestsQuery = GetEntityQuery(typeof(CraftRequest));
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
                
                Entities.ForEach((ref CraftRequest request) =>
                {
                    if (request.State != CraftReceiptState.Pending && request.State != CraftReceiptState.Processing)
                    {
                        return;
                    }

                    if (arenaState.State != ArenaMatchState.Preparing)
                    {
                        request.State = CraftReceiptState.CrafterUnavailable;
                    }

                }).Run();
            }
            
            var overlappingBuffers = GetBufferLookup<OverlappingEntities>(true);
            
            Entities
                .WithReadOnly(overlappingBuffers)
                .ForEach((ref CraftRequest request) =>
            {
                if (request.State != CraftReceiptState.Pending && request.State != CraftReceiptState.Processing)
                {
                    return;
                }

                if (SystemAPI.HasComponent<InteractiveObject>(request.Crafter) == false)
                {
                    return;
                }

                if (overlappingBuffers.HasBuffer(request.InventoryOwner) == false)
                {
                    return;
                }

                var overlappings = overlappingBuffers[request.InventoryOwner];
                bool isInteracting = false;
                
                foreach (var overlapping in overlappings)
                {
                    if (SystemAPI.HasComponent<InteractiveObject>(overlapping.Entity) == false)
                    {
                        continue;
                    }
                    
                    if (overlapping.Entity == request.Crafter)
                    {
                        isInteracting = true;
                        break;
                    }
                }

                if (isInteracting == false)
                {
                    request.State = CraftReceiptState.CrafterUnavailable;
                }
                
            }).Run();
        }
    }
}
