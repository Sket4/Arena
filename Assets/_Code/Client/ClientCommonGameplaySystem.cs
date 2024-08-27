using Arena.ScriptViz;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [UpdateBefore(typeof(CommonEarlyGameSystem))]
    [DisableAutoCreation]
    public partial class ClientCommonGameplaySystem : GameSystemBase
    {
        private EntityQuery startQuestQuery;
        protected EntityQuery gameInterfaceQuery;
        
        public struct QuestStartedFlag : IComponentData
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            startQuestQuery = GetEntityQuery(ComponentType.ReadOnly<StartQuestRequest>());
            gameInterfaceQuery = GetEntityQuery(ComponentType.ReadOnly<GameInterface>());
        }

        protected override void OnSystemUpdate()
        {
            if (startQuestQuery.IsEmpty == false)
            {
                if (SystemAPI.HasSingleton<QuestStartedFlag>())
                {
                    Debug.LogError("quest already started");
                    EntityManager.DestroyEntity(startQuestQuery);
                    return;
                }
                
                var startRequests = startQuestQuery.ToComponentDataArray<StartQuestRequest>(Allocator.Temp);

                if (startRequests.Length > 1)
                {
                    Debug.LogWarning("Слишком много запросов на начало квеста, будет использован последний в списке");
                }
                var request = startRequests[startRequests.Length - 1];

                var mainDbEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
                var db = SystemAPI.GetBuffer<IdToEntity>(mainDbEntity);

                IdToEntity.TryGetEntityById(db, request.QuestID, out var questEntity);

                if (questEntity != Entity.Null)
                {
                    var gameInterface = gameInterfaceQuery.GetSingleton<GameInterface>();

                    int spawnPointId = 0;

                    if (SystemAPI.HasComponent<SpawnPointIdData>(questEntity))
                    {
                        spawnPointId = SystemAPI.GetComponent<SpawnPointIdData>(questEntity).ID;
                    }

                    EntityManager.CreateSingleton<QuestStartedFlag>(new FixedString64Bytes("quest started flag"));
				
                    _ = gameInterface.StartQuest(new QuestGameInfo
                    {
                        GameSceneID = SystemAPI.GetComponent<Quests.QuestData>(questEntity).GameSceneID,
                        SpawnPointID = spawnPointId,
                        MatchType = "ArenaMatch",
                        Multiplayer = false
                    });
                }
                else
                {
                    Debug.LogError($"Failed to find quest with id {request.QuestID} in database {mainDbEntity}");
                }
                
                EntityManager.DestroyEntity(startQuestQuery);
            }
        }
    }
}
