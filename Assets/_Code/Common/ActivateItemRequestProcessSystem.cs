using Arena.Server;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(ActivateItemRequestSystem))]
    [UpdateInGroup(typeof(ItemActivationSystemGroup))]
    public partial class ActivateItemRequestProcessSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            
            Entities.ForEach((int entityInQueryIndex, in ActivateItemRequest request) =>
            {
                if (request.State != ActivateItemRequestState.Success)
                {
                    return;
                }

                if (SystemAPI.HasComponent<Item>(request.Item) == false)
                {
                    return;
                }

                var item = SystemAPI.GetComponent<Item>(request.Item);

                if (SystemAPI.HasComponent<PlayerController>(item.Owner) == false)
                {
                    Debug.Log("Failed to save player data on item activation, no player controller");
                    return;
                }

                var playerEntity = SystemAPI.GetComponent<PlayerController>(item.Owner).Value;

                if (SystemAPI.HasComponent<AuthorizedUser>(playerEntity) == false)
                {
                    Debug.Log("Failed to save player data on item activation, player not authorized");
                    return;
                }
                
                Debug.Log($"Saving data for player character {item.Owner.Index}, bcz item activated");
                var playerId = SystemAPI.GetComponent<AuthorizedUser>(playerEntity).Value;
                
                var requestEntity = commands.CreateEntity(entityInQueryIndex);
                
                commands.AddComponent(entityInQueryIndex, requestEntity, new PlayerDataSaveRequest
                {
                    Owner = Entity.Null,
                    PlayerId = playerId,
                    State = PlayerDataRequestState.Pending
                });
                commands.AddComponent(entityInQueryIndex, requestEntity, new Target { Value = playerEntity });
                commands.AddComponent(entityInQueryIndex, requestEntity, new PlayerSaveData
                {
                    CharacterEntity = item.Owner
                });

            }).Run();
        }
    }    
}

