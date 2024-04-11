using System.Collections.Generic;
using System.Threading.Tasks;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Database;
using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena.Server
{
    [DisableAutoCreation]
    public partial class PlayerDataOnlineStoreSystem : PlayerDataStoreBaseSystem
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        ServiceWrapper<GameDatabaseService.GameDatabaseServiceClient> dbClient;

        public PlayerDataOnlineStoreSystem(ServiceWrapper<GameDatabaseService.GameDatabaseServiceClient> dbClient)
        {
            this.dbClient = dbClient;
        }

        //struct TaskInfo
        //{
        //    public Task Task;
        //    public Entity[] RequestEntities;

        //    public bool ContainsRequestEntity(Entity entity)
        //    {
        //        for(int i=0; i<RequestEntities.Length; i++)
        //        {
        //            if(RequestEntities[i] == entity)
        //            {
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //}

        //List<TaskInfo> tasks = new List<TaskInfo>();
        //List<TaskInfo> readyTasks = new List<TaskInfo>();

        //protected override void OnDestroy()
        //{
        //    base.OnDestroy();

        //    Task[] tasksToWait = new Task[tasks.Count];

        //    for (int i = 0; i < tasks.Count; i++)
        //    {
        //        TaskInfo taskInfo = tasks[i];
        //        tasksToWait[i] = taskInfo.Task;
        //    }

        //    Task.WaitAll(tasksToWait);
        //}

        //protected override void OnUpdate()
        //{
        //    base.OnUpdate();

        //    for (int i = tasks.Count-1; i >= 0; i--)
        //    {
        //        var task = tasks[i];

        //        if (task.Task.IsCompleted)
        //        {
        //            //log.Info("task completed {0}", task.Task.ToString());
        //            readyTasks.Add(task);
        //            tasks.RemoveAt(i);
        //        }
        //    }

        //    processLoadRequests();
        //    procesSaveRequests();

        //    if(readyTasks.Count > 0)
        //    {
        //        for (int i = 0; i < readyTasks.Count; i++)
        //        {
        //            TaskInfo task = readyTasks[i];

        //            var sb = new System.Text.StringBuilder();
        //            foreach(var entity in task.RequestEntities)
        //            {
        //                sb.Append(entity);
        //                sb.Append(',');
        //            }
        //            UnityEngine.Debug.LogWarningFormat("Failed to find entity for data load task for entities {0}", sb.ToString());
        //        }
        //    }

        //    readyTasks.Clear();
        //}

        //Dictionary<Entity, Entity> playersToSaveRequests = new Dictionary<Entity, Entity>();

        //void procesSaveRequests()
        //{
        //    playersToSaveRequests.Clear();

        //    Entities.ForEach((Entity entity, ref PlayerDataSaveRequest request) =>
        //    {
        //        if (request.State == PlayerDataRequestState.Failed || request.State == PlayerDataRequestState.Success)
        //        {
        //            return;
        //        }

        //        if (request.State == PlayerDataRequestState.Pending)
        //        {
        //            playersToSaveRequests.Add(entity, request.Player);
        //            request.State = PlayerDataRequestState.Running;

        //            return;
        //        }

        //        for (int i = 0; i < readyTasks.Count; i++)
        //        {
        //            var taskInfo = readyTasks[i];

        //            if (taskInfo.ContainsRequestEntity(entity))
        //            {
        //                request.State = PlayerDataRequestState.Success;
        //                readyTasks.RemoveAt(i);
        //                break;
        //            }
        //        }
        //    });

        //    if(playersToSaveRequests.Count == 0)
        //    {
        //        return;
        //    }

        //    var client = DatabaseGameLib.DatabaseUtility.CreateDatabaseClient();
        //    var dbRequest = new DbSaveCharactersRequest();
        //    var requestEntities = new Entity[playersToSaveRequests.Count];
        //    var requesIndex = 0;

        //    foreach(var request in playersToSaveRequests)
        //    {
        //        var data = EntityManager.GetComponentData<PlayerData>(request.Key).Data as CharacterData;
        //        data.ID = EntityManager.GetComponentData<CharacterDataId>(request.Value).Value;

        //        dbRequest.Characters.Add(data);
        //        requestEntities[requesIndex] = request.Key;
        //        requesIndex++;
        //    }

        //    var task = Task.Run(async () =>
        //    {
        //        return await client.SaveCharactersAsync(dbRequest);
        //    });

        //    log.Info("Creating save task");
        //    tasks.Add(new TaskInfo() { RequestEntities = requestEntities, Task = task });
        //}

        //void processLoadRequests()
        //{
        //    Entities.ForEach((Entity entity, ref PlayerDataLoadRequest request, ref AuthorizedUser authorizedUser) =>
        //    {
        //        if (request.State == PlayerDataRequestState.Failed || request.State == PlayerDataRequestState.Success)
        //        {
        //            return;
        //        }

        //        if (request.State == PlayerDataRequestState.Pending)
        //        {
        //            var userId = authorizedUser.Value;

        //            var newTask = Task.Run(async () =>
        //            {
        //                UnityEngine.Debug.Log("Entered loading data fro player " + userId);
        //                var client = DatabaseGameLib.DatabaseUtility.CreateDatabaseClient();
        //                var dbRequest = new GetCharacterRequest();
        //                dbRequest.AccountId = new AccountId();
        //                dbRequest.AccountId.Value = userId.Value;
        //                return await client.GetSelectedCharacterForAccountAsync(dbRequest);
        //            });

        //            tasks.Add(new TaskInfo() { RequestEntities = new Entity[] { entity }, Task = newTask });
        //            request.State = PlayerDataRequestState.Running;
        //            UnityEngine.Debug.Log("Started loading data fro player " + authorizedUser.Value);

        //            return;
        //        }

        //        TaskInfo targetTaskInfo = default;

        //        for (int i = 0; i < readyTasks.Count; i++)
        //        {
        //            var taskInfo = readyTasks[i];

        //            if (taskInfo.Task is Task<GetSelectedResult> == false)
        //            {
        //                continue;
        //            }

        //            if (taskInfo.ContainsRequestEntity(entity))
        //            {
        //                targetTaskInfo = taskInfo;
        //                readyTasks.RemoveAt(i);
        //                break;
        //            }
        //        }

        //        if (targetTaskInfo.RequestEntities == null || targetTaskInfo.RequestEntities.Length == 0)
        //        {
        //            return;
        //        }

        //        var task = targetTaskInfo.Task as Task<GetSelectedResult>;

        //        if (task.Result == null)
        //        {
        //            request.State = PlayerDataRequestState.Failed;
        //            UnityEngine.Debug.LogWarningFormat("Failed to load data for UserID: {0}", authorizedUser.Value);
        //            return;
        //        }

        //        request.State = PlayerDataRequestState.Success;
        //        PostUpdateCommands.AddComponent(entity, new CharacterDataId { Value = task.Result.Character.ID });
        //        PostUpdateCommands.AddComponent(entity, new PlayerData { Data = task.Result.Character });
        //    });
        //}

        protected override async Task<object> LoadPlayerData(AuthorizedUser authorizedUser)
        {
            UnityEngine.Debug.Log("Entered loading data for player " + authorizedUser.Value);
            
            var dbRequest = new GetCharacterRequest();
            dbRequest.AccountId = new AccountId();
            dbRequest.AccountId.Value = authorizedUser.Value.Value;
            var result = await dbClient.Service.GetSelectedCharacterForAccountAsync(dbRequest);
            if(result == null || result.Character == null)
            {
                UnityEngine.Debug.LogError($"Failed to load data for player {authorizedUser.Value}");
                return null;
            }
            UnityEngine.Debug.Log($"Data loaded for player {authorizedUser.Value}");
            return result.Character;
        }

        protected override async Task<object> SavePlayerData(PlayerId playerId, Dictionary<string, object> playerData)
        {
            var dbRequest = new DbSaveCharactersRequest();

            dbRequest.Characters.Add(playerData["CharacterData"] as CharacterData);
            var result = await dbClient.Service.SaveCharactersAsync(dbRequest);
            return Task.FromResult(new object());
        }

        protected override void GetSaveDataFromRequestEntity(Entity requestEntity, Dictionary<string, object> data)
        {
            var saveData = GetComponent<PlayerSaveData>(requestEntity);
            var characterData = PlayerDataStoreUtility.CreateCharacterDataFromEntity(saveData.CharacterEntity, EntityManager);
            data.Add("CharacterData", characterData);

            // заодно обновляем данные игрока
            var targetEntity = GetComponent<Target>(requestEntity).Value;
            
            if (EntityManager.Exists(targetEntity))
            {
                var playerData = EntityManager.GetComponentData<PlayerData>(targetEntity);
                playerData.Data = characterData;    
            }
             
            //var matchType = EntityManager.GetComponentData<MatchTypeData>(requestEntity);
            //data.Add(matchTypeKey, matchType);
            //var commonMatchData = EntityManager.GetComponentData<CommonMatchSaveData>(requestEntity);
            //data.Add(commonMatchDataKey, commonMatchData);

            //switch (matchType.Value)
            //{
            //    case MatchType.Speedrun:
            //        {
            //            var matchData = EntityManager.GetComponentData<SpeedrunMatchSaveData>(requestEntity);
            //            data.Add(speedrunMatchDataKey, matchData);
            //        }
            //        break;

            //    case MatchType.MultiplayerGame:
            //        {
            //            var matchData = EntityManager.GetComponentData<MultiplayerMatchSaveData>(requestEntity);
            //            data.Add(multiMatchDataKey, matchData);
            //        }
            //        break;
            //}
        }
    }
}
