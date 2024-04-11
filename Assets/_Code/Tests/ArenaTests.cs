using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TzarGames.MultiplayerKit;
using Arena.Client;
using Unity.Entities;
using Arena;
using Arena.Server;
using TzarGames.GameCore;
using System.Threading.Tasks;
using TzarGames.GameCore.Abilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Transforms;
using TzarGames.MultiplayerKit.Server;
using TzarGames.MultiplayerKit.Client;
using TzarGames.MatchFramework.Server;
using TzarGames.MatchFramework.Client;
using TzarGames.MatchFramework.Database;
using TzarGames.MatchFramework;
using System.Threading;

namespace Arena.Tests
{
    public abstract class TesterBase : System.IDisposable
    {
        public TesterBase(string name)
        {
            Name = name;
        }
        public virtual void Update() 
        {
#if UNITY_EDITOR
            //Debug.LogFormat("Tick {0} {1}", tickCount, World.Name);
            tickCount++;
#endif
        }
        public string Name { get; private set; }
        public abstract INetworkInfoProvider Net { get; }
        public abstract NetworkRpcSystem RpcSystem { get; }
        public abstract World World { get; }

#if UNITY_EDITOR
        int tickCount = 0;
#endif

        public void CreateNetworkObject(NetworkID networkID, out NetworkIdentity identity, out Entity entity)
        {
            entity = World.EntityManager.CreateEntity();
            identity = new NetworkIdentity();
            World.EntityManager.AddComponentObject(entity, identity);
            identity.Initialize(networkID, RpcSystem, default);
        }

        public abstract void Dispose();
    }

    public class ServerTester : TesterBase
    {
        public GameServerLoop Server;
        ServerSystem serverSystem;
        NetworkRpcSystem rpcSystem;
        public NetworkIdentitySystem IdSystem { get; private set; }
        public ushort DefaultPort = 9000;

        public ServerSystem ServerSystem
        {
            get
            {
                return serverSystem;
            }
        }

        public ServerTester(string name, GameServerLoop server) : base(name)
        {
            Server = server;
            serverSystem = Server.World.GetOrCreateSystemManaged<ServerSystem>();
            rpcSystem = Server.World.GetOrCreateSystemManaged<NetworkRpcSystem>();
            IdSystem = Server.World.GetOrCreateSystemManaged<NetworkIdentitySystem>();
        }

        public override INetworkInfoProvider Net => serverSystem;

        public override NetworkRpcSystem RpcSystem => rpcSystem;

        public override World World => Server.World;

        public void Start()
        {
            Server.Start(DefaultPort);
        }

        public override void Update()
        {
            base.Update();

            Server.Update();
        }

        public override void Dispose()
        {
            Server.Dispose();
        }
    }

    public class ClientTester : TesterBase
    {
        public GameClient Client { get; private set; }
        public string AuthToken { get; set; }
        ClientSystem clientSystem;
        NetworkRpcSystem rpcSystem;

        public override INetworkInfoProvider Net => clientSystem;

        public override NetworkRpcSystem RpcSystem => rpcSystem;
        public ClientSystem ClientSystem => clientSystem;

        public override World World => Client.World;

        float disconnectTime = -1;
        float disconnectStartTime = 0;
        bool disconnectTimerSet = false;

        public ClientTester(string name, bool bot, ClientGameSettings gameSettings, Unity.Entities.Hash128[] additionalScenes = null) : base(name)
        {
            Client = new GameClient(name, bot, gameSettings, additionalScenes, true);
            clientSystem = Client.World.GetOrCreateSystemManaged<ClientSystem>();
            rpcSystem = Client.World.GetOrCreateSystemManaged<NetworkRpcSystem>();
        }

        public void Connect(string ip = "127.0.0.1", ushort port = 9000)
        {
            Client.ConnectToServer(ip, port);
        }

        public override void Update()
        {
            base.Update();

            if(disconnectTimerSet)
            {
                if(Time.time - disconnectStartTime >= disconnectTime)
                {
                    disconnectTimerSet = false;
                    Client.Disconnect();
                }
            }
            Client.Update();
        }

        public void RandomDisconnect(float maxTime)
        {
            disconnectStartTime = Time.time;
            disconnectTime = Random.Range(0, maxTime);
            disconnectTimerSet = true;
        }

        public override void Dispose()
        {
            Client.Dispose();
        }
    }

    public class TesterFactory : IServerLauncher
    {
        List<TesterBase> testers = new List<TesterBase>();
        public static ServerGameSettings ServerSettings = null;
        static ClientGameSettings clientSettings = null;
        static TestGameType testGameType;

        public TesterBase this[int i]
        {
            get
            {
                if(testers.Count >= i)
                {
                    return null;
                }
                return testers[i];
            }
        }

        public Invoker Invoker { get; } = new Invoker();

        public static IEnumerator LoadAssets()
        {
            Debug.LogError("Not implemented");
            yield break;
            //if(ServerSettings == null)
            //{
            //    var serverLoading = Addressables.LoadAssetAsync<ServerGameSettings>("Server settings");
            //    while(serverLoading.IsDone == false)
            //    {
            //        yield return null;
            //    }
            //    ServerSettings = serverLoading.Result;
            //}

            //if (clientSettings == null)
            //{
            //    var clientLoading = Addressables.LoadAssetAsync<ClientGameSettings>("Client settings");
            //    while (clientLoading.IsDone == false)
            //    {
            //        yield return null;
            //    }
            //    clientSettings = clientLoading.Result;
            //}

            //if(testGameType == null)
            //{
            //    var testLoading = Addressables.LoadAssetAsync<TestGameType>("Test game type");
            //    while (testLoading.IsDone == false)
            //    {
            //        yield return null;
            //    }
            //    testGameType = testLoading.Result;
            //}
        }
        
        public ServerTester CreateTestMatchServer()
        {
            var request = new ServerGameRequest();
            request.UserRequests.Add(new UserRequest { UserId = new AccountId { Value = 1 } });
            testGameType.Initialize(this);
            var serverTask = testGameType.HandleGameRequest(request);
            serverTask.Wait();
            var server = serverTask.Result;
            return GetTesterFromGameServer(server.GameServer as GameServerLoop) as ServerTester;
        }

        public void SetPortRange(ushort min, ushort max)
        {
            //
        }

        public ServerTester CreateServerTester(string name, ServerGameSettings settings = null, Unity.Entities.Hash128[] additionalScenes = null)
        {
            var server = CreateServer(name, settings != null ? settings : ServerSettings, additionalScenes);
            return GetTesterFromGameServer(server) as ServerTester;
        }

        public ClientTester CreateClient(string name, bool bot, ClientGameSettings gameSettings = null, Unity.Entities.Hash128[] additionalScenes = null)
        {
            if (gameSettings == null)
            {
                gameSettings = clientSettings;
            }
            var client = new ClientTester(name, bot, gameSettings, additionalScenes);
            testers.Add(client);
            return client;
        }
        
        public List<ServerTester> GetServerTesters()
        {
            var result = new List<ServerTester>();

            foreach(var tester in testers)
            {
                if(tester is ServerTester)
                {
                    result.Add(tester as ServerTester);
                }
            }

            return result;
        }

        List<TesterBase> toDestroy = new List<TesterBase>();

        public void Update()
        {
            Invoker.RunPendingActions();

            foreach (var tester in testers)
            {
                tester.Update();

                if(tester is ServerTester)
                {
                    if((tester as ServerTester).Server.PendingShutdown)
                    {
                        toDestroy.Add(tester);
                    }
                }
                else if(tester is ClientTester)
                {
                    if ((tester as ClientTester).Client.PendingDestroy)
                    {
                        toDestroy.Add(tester);
                    }
                }
            }

            if(toDestroy.Count > 0)
            {
                foreach(var tester in toDestroy)
                {
                    Debug.LogFormat("Уничтожение {0}", tester.Name);
                    Destroy(tester);
                }
                toDestroy.Clear();
            }
        }

        public IEnumerator UpdateForSeconds(float seconds)
        {
            float startTime = Time.time;
            while(Time.time - startTime < seconds)
            {
                Update();
                yield return null;
            }
        }

        public void Destroy(TesterBase tester)
        {
            if (testers.Contains(tester))
            {
                testers.Remove(tester);
                tester.Dispose();
            }
        }

        public void DestroyAll()
        {
            foreach(var tester in testers)
            {
                try
                {
                    tester.Dispose();
                }
                catch(System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            testers.Clear();
        }

        IGameServer IServerLauncher.CreateServer(IServerGameSettings gameSettings)
        {
            return CreateServer(gameSettings.ServerName, (gameSettings is ServerGameSettings ? (gameSettings as ServerGameSettings) : null), null);
        }

        GameServerLoop CreateServer(string serverName, ServerGameSettings gameSettings, Unity.Entities.Hash128[] additionalScenes)
        {
            //(this as IServerLauncher) 
            if (gameSettings == null)
            {
                gameSettings = ServerSettings;
            }
            
            var server = new GameServerLoop(gameSettings, additionalScenes, null, null);
            var tester = new ServerTester(serverName, server);
            testers.Add(tester);

            return server;
        }

        public TesterBase GetTesterFromGameServer(GameServerLoop server)
        {
            foreach(var tester in testers)
            {
                if(tester.World == server.World)
                {
                    return tester;
                }
            }
            return null;
        }

        public float GetLoad()
        {
            throw new System.NotImplementedException();
        }

        public Task<GameSessionStatusResult> GetGameSessionStatus(string gameSessionId, PlayerId[] playerIds, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }

    static class TestExtensions
    {
        public static IEnumerator AsIEnumeratorReturnNull<T>(this Task<T> task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                yield break;
            }

            yield return null;
        }
    }

    public class ArenaTests
    {
        void createLevel()
        {
            var floor = Resources.Load("Test Floor");
            var floorInstance = Object.Instantiate(floor) as GameObject;
            
            var spawnPoint = new GameObject("Spawn point");
            spawnPoint.tag = "Spawn point";
            spawnPoint.transform.position = floorInstance.transform.position + Vector3.up;
        }

        
        [UnityTest]
        public IEnumerator BasicClientServer()
        {
            createLevel();

            var factory = new TesterFactory();
            yield return TesterFactory.LoadAssets();

            var client1 = factory.CreateClient("My Client 1", true);
            //var client2 = factory.CreateClient("My Client 2", true);
            var server = factory.CreateServerTester("My server");

            server.Start();
            factory.CreateTestMatchServer();

            client1.Connect();

            //client2.Connect();
            //client2.Client.InitializeMatch();

            yield return factory.UpdateForSeconds(3f);
            client1.Client.Disconnect();
            yield return factory.UpdateForSeconds(1f);
            //client2.Client.Disconnect();
            yield return factory.UpdateForSeconds(1f);
            client1.Connect();
            yield return factory.UpdateForSeconds(1f);
            //client2.Connect();
            yield return factory.UpdateForSeconds(2f);
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator MassConnectDisconnect()
        {
            createLevel();

            var factory = new TesterFactory();
            yield return TesterFactory.LoadAssets();

            
            factory.CreateTestMatchServer();

            for(int i=0; i<10; i++)
            {
                var client = factory.CreateClient("My Client " + i, true);
                client.Connect();
                
                if(i == 5)
                {
                    client.RandomDisconnect(0);
                }
                else
                {
                    client.RandomDisconnect(15);
                }
                
                yield return factory.UpdateForSeconds(0.1f);
            }

            yield return factory.UpdateForSeconds(20f);

            
        }

        [UnityTest]
        public IEnumerator authTest()
        {
            Debug.Log("Authenticating to firebase");
            var signInTask = Firebase.Auth.FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();

            while(signInTask.IsCompleted == false)
            {
                yield return null;
            }

            if (signInTask.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                yield break;
            }
            if (signInTask.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + signInTask.Exception);
                yield break;
            }

            Debug.Log("retrieving the token");

            var tokenTask = signInTask.Result.User.TokenAsync(true);

            while(tokenTask.IsCompleted == false)
            {
                yield return null;
            }

            Debug.Log("trying to auth on the server");

            yield return null;

            var client = ClientUtility.GetAuthService(SharedValues.AuthServerIP, SharedValues.AuthServerPort);
            var request = new FirebaseAuthRequest();
            request.IdToken = tokenTask.Result;
            var result = client.Service.AuthenticateUsingFirebase(request);

            Debug.LogFormat("result {0} token: {1}", result.Success, result.AuthToken);

            yield break;
        }

        static class SharedValues
        {
            public const string AuthServerIP = "127.0.0.1";
            public const int AuthServerPort = 60600;
        }

        class ClientTask
        {
            public Task Task;
            public ClientTester Client;
            public float StartTime;
            public float CompletedTime;
        }

        [UnityTest]
        public IEnumerator matchServerRegisterTest()
        {
            
            
            Debug.Log("Running requests");
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var frontendClient = ServerUtility.GetInternalFrontendServiceClient("127.0.0.1", ServerUtility.DefaultInternalFrontendServicePort, null);
                var request = new RegisterGameServerRequest { Port = i, UseEncryption = false };
                request.GameTypes.Add("Unity Test");
                var task = frontendClient.Service.RegisterGameServerAsync(request);
                tasks.Add(task.ResponseAsync);
            }

            Debug.Log("waiting tasks");
            while (tasks.Count > 0)
            {
                for (var index = tasks.Count - 1; index >= 0; index--)
                {
                    var task = tasks[index];
                    
                    if (task.IsCompleted)
                    {
                        tasks.Remove(task);
                    }
                }

                yield return null;
            }
        }
        
        [UnityTest]
        public IEnumerator matchServerLoadTest()
        {
            var gameType = ScriptableObject.CreateInstance<TestEmptyGameType>();

            var factory = new TesterFactory();
            yield return TesterFactory.LoadAssets();

            gameType.ServerGameSettings = TesterFactory.ServerSettings;

            var serverObject = new GameObject("Match server");
            var matchServer = serverObject.AddComponent<MatchService>();
            matchServer.GameTypes = new List<GameType>();
            matchServer.GameTypes.Add(gameType);
            matchServer.GameLauncher = factory;

            var loadTask = matchServer.Load();
            while(loadTask.IsCompleted == false)
            {
                yield return null;
            }

            yield return factory.UpdateForSeconds(1);

            var tasks = new List<ClientTask>();

            for(int i=0; i<10; i++)
            {
                var client = factory.CreateClient("Client " + i, false);
                var task = AuthenticateClient(client);
                var info = new ClientTask
                {
                    Client = client,
                    Task = task,
                    StartTime = Time.realtimeSinceStartup
                };
                tasks.Add(info);
            }

            var tasksToComplete = new List<ClientTask>(tasks);

            while(tasksToComplete.Count > 0)
            {
                for (int i = tasksToComplete.Count-1; i >= 0; i--)
                {
                    var t = tasksToComplete[i];

                    if (t.Task.IsCompleted)
                    {
                        t.CompletedTime = Time.realtimeSinceStartup - t.StartTime;
                        Debug.LogFormat("auth task completed for {0}, time: {1}", i, t.CompletedTime);
                        tasksToComplete.RemoveAt(i);
                    }
                }
                factory.Update();
                yield return null;
            }

            float minAuthTime = float.MaxValue;
            float maxAuthTime = float.MinValue;

            foreach(var task in tasks)
            {
                if(task.CompletedTime > maxAuthTime)
                {
                    maxAuthTime = task.CompletedTime;
                }
                if(task.CompletedTime < minAuthTime)
                {
                    minAuthTime = task.CompletedTime;
                }
            }

            Debug.LogFormat("Min auth time: {0}", minAuthTime);
            Debug.LogFormat("Max auth time: {0}", maxAuthTime);
        }

        class TaskInfo
        {
            public Task Task;
            public float StartTime;
            public float CompleteTime;
        }

        [UnityTest]
        public IEnumerator simplePingTest()
        {
            var tasks = new List<TaskInfo>();

            for(int i=0; i<100; i++)
            {
                var client = TestLib.TestUtility.GetTestClient();
                var request = new Client.Tests.PingRequest
                {
                };
                var task = client.PingAsync(request).ResponseAsync;
                var info = new TaskInfo()
                {
                    Task = task,
                    StartTime = Time.realtimeSinceStartup
                };
                tasks.Add(info);
                Debug.Log("Ping started");
            }
            
            while(true)
            {
                bool stop = true;

                foreach (var task in tasks)
                {
                    if(task.Task.IsCompleted == false)
                    {
                        stop = false;
                    }
                    else
                    {
                        if(task.CompleteTime <= 0.0f)
                        {
                            Debug.Log("Ping completed");
                            task.CompleteTime = Time.realtimeSinceStartup;
                        }
                    }
                }

                if(stop)
                {
                    break;
                }
                
                yield return null;
            }

            float minPingTime = float.MaxValue;
            float maxPingTime = float.MinValue;

            foreach (var task in tasks)
            {
                if (task.CompleteTime > maxPingTime)
                {
                    maxPingTime = task.CompleteTime;
                }
                if (task.CompleteTime < minPingTime)
                {
                    minPingTime = task.CompleteTime;
                }
            }

            Debug.LogFormat("Min ping time: {0}", minPingTime);
            Debug.LogFormat("Max ping time: {0}", maxPingTime);
        }

        static readonly string[] testUserIds = new string[]
        {
            "8a238dc5-49cc-4516-9add-d8518bd283d4",
            "67214bd9-583c-4ff5-8957-c89b0f5a4556",
            "ddde3b9b-7269-4e8d-9fc2-0e5041eeeb54",
            "22fb204c-df51-4cc5-aaf8-cd0f1d177e05",
            "c8ae91be-c8f4-4e0b-9e34-cd47aface0cb",
            "91e9ca37-af5c-496c-826a-5ca14dc84ecc",
            "ad5c3a0f-2c15-4c3b-92ba-8563b1cc4dee",
            "b146c69c-2c98-44bb-8b37-9099d9cd32b8",
            "a0bc0311-9d5b-41b6-9541-236408e25281",
            "30ced69b-9c8f-4d59-a3e4-cdabea2a8b8d",
            "e5c49d8c-f421-4da0-ab01-453ad0d8a56d",
            "d1468809-f360-4292-97c7-45036b7a1d70",
            "5e8be5b0-1d13-4133-9fe9-e3fe1727ca00",
            "f256eec8-700d-46e5-857c-39b7887bcc26",
            "0b6aaf67-4ad7-42b2-9bc3-6516e51ebd6f",
            "1df3239e-b5a0-4ffd-9a3c-a650c0f318d8",
            "89c78512-0ddf-4d31-b1a7-3100789cf68b",
            "c1777f78-be42-4fce-9b01-8329cbf87c5d",
            "2c7a7be0-a8a8-4e44-8620-0c9b7b78fb8e",
            "f515eba3-5b51-4602-936f-e5563c8d99fc",
            "f6b4c213-d514-4b48-8765-508396e2887b",
            "26b98468-b648-4e90-b343-ac2d6a56034b",
            "5545db7b-a474-4e9c-8e51-910225cb405d",
            "9a284a45-faeb-496c-b56e-092543149586",
            "570e42e3-9d07-4421-b43c-3c409d9b31a4",
            "713ae215-9b5d-4745-b4e2-e55e99df0150",
            "30076f20-7b35-4be8-8305-dde14256f51b",
            "09d009a9-9d6d-4c5d-b662-0b72d87a654a",
            "3492cc1d-f9d0-4fa9-a3c2-f2aa54bf8e18",
            "ce30f2cb-b6f9-495e-8203-91f5b9aefd89",
            "651e5fd0-022b-486b-8386-130a6e0b741e",
            "34d69b30-4a3c-4d80-93de-fb2ceb501a45",
            "9af4a742-b599-4c17-b8c9-eca4b015200f",
            "69fb9b20-cd3e-4f61-8808-7ecbdf419ce6",
            "9160c90f-3d33-4f93-88e0-7b0d48f7d09a",
            "03663179-6687-4261-90fa-8c6369c85406",
            "ea2799de-30db-4096-950e-63ae1464bfaa",
            "e0c94b00-fe88-463d-88be-a0ac2c90461c",
            "8e6b3abf-6ffa-4d5f-b3aa-afe64ad74d1c",
            "d5570d67-b26b-4d0b-a605-dbf7f64c08ee",
            "921935ea-e443-4242-81ab-57f49d0551c6",
            "650485e0-40f7-4dc3-ad27-233e1a9de4cd",
            "04aace97-6e1f-4e99-ad0c-cd5427970072",
            "07f5cc5e-779c-4fc8-813f-988814541384",
            "48122a35-65e9-4214-8aa7-708ab34d8959",
            "3101b4bc-deb9-4c47-8b41-86d5e8bd3911",
            "caf8e84a-68a5-459c-abdc-0fa8c95bc74e",
            "3772c0cd-a46f-4b77-81ce-c3bd1f580038",
            "27acace2-0e35-4d42-a2ea-9c9c59c80e4c",
            "0f0f05fb-d80e-434a-9a11-6fc5d10effd8",
        };

        static int currentUserId = 0;

        public static async Task<string> GetTestAuthToken()
        {
            var userId = testUserIds[currentUserId];
            var testToken = await GetTestToken(userId);

            if (string.IsNullOrEmpty(testToken))
            {
                Debug.LogError("Failed to get custom token");
                return null;
            }

            var token = await Authentication.GetFirebaseTokenByCustomToken(testToken);

            currentUserId++;

            if (currentUserId >= testUserIds.Length)
                currentUserId = 0;

            return token;
        }

        static async Task<ClientTester> AuthenticateClient(ClientTester client)
        {
            var token = await GetTestAuthToken();

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("Failed to get firebase token by custom token");
                return client;
            }

            var authToken = await Authentication.AuthenticateUsingFirebaseToken(token, null);

            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("Failed to get auth token");
                return client;
            }
            Debug.LogFormat("Auth complete for {0} \n fireToken {1} \n authToken {2}", client.Name, token, authToken);
            client.AuthToken = authToken;
            
            return client;
        }

        static async Task<string> GetTestToken(string userId)
        {
            var client = TestLib.TestUtility.GetTestClient();
            var request = new Client.Tests.TokenRequest
            {
                UserId = userId
            };
            var result = await client.GetTokenAsync(request);
            return result.AuthToken;
        }

        [UnityTest]
        public IEnumerator abilitySystemTest()
        {
            throw new System.NotImplementedException("нужна имплементация job для умения, так как по умолчанию они генерятся из префаба");

            var world = new World("Ability system test world");
            var timeSystem = world.CreateSystemManaged<TimeSystem>();
            var system = world.CreateSystemManaged<AbilitySystem>();
            
            var groupSystem = world.CreateSystemManaged<TestSystemGroup>();
            groupSystem.AddSystemToUpdateList(timeSystem);
            groupSystem.AddSystemToUpdateList(system);
            
            var entity = world.EntityManager.CreateEntity(typeof(TestAbilityComponent), typeof(AbilityState));
            world.EntityManager.SetComponentData(entity, new TestAbilityComponent { Value = 0 });
            world.EntityManager.SetComponentData(entity, new AbilityState { Value = AbilityStates.Idle });

            int iterations = 0;
            
            groupSystem.Update();
            
            while (iterations < 4)
            {
                groupSystem.Update();
                world.EntityManager.SetComponentData(entity, new AbilityState { Value = AbilityStates.Idle });
                iterations++;
                yield return null;
            }

            var val = world.EntityManager.GetComponentData<TestAbilityComponent>(entity).Value;
            Assert.IsTrue(val == 13, $"Value != 13, but {val}");

            world.EntityManager.AddComponentData(entity, new TestAbilityActionCounter());
            var events = world.EntityManager.AddBuffer<TestAbilityAction>(entity);
            events.Add(new TestAbilityAction { Message = "Tratata", CallerId = 123, ActionId = 1 });
            
            groupSystem.Update();
            yield return null;
            groupSystem.Update();

            var counter = world.EntityManager.GetComponentData<TestAbilityActionCounter>(entity);
            Assert.IsTrue(counter.Value == 1, $"Counter != 1, but {counter.Value}");
        }
        
        [UnityTest]
        public IEnumerator timerEventAbilityComponentTest()
        {
            float time = 6;
            
            var world = new World("Ability system test world");
            var systemGroup = world.CreateSystemManaged<TestSystemGroup>();
            var timeSystem = world.CreateSystemManaged<TimeSystem>();
            var system = world.CreateSystemManaged<AbilitySystem>();
            systemGroup.AddSystemToUpdateList(timeSystem);
            systemGroup.AddSystemToUpdateList(system);

            var abilityGO = new GameObject("Ability");
            var ability = abilityGO.AddComponent<Ability>();
            var durationComponent = ability.gameObject.AddComponent<DurationComponent>();
            var timerEventComponentAsset = ability.gameObject.AddComponent <TimerEventAbilityComponent>();
            
            durationComponent.Value = new Duration
            {
                BaseValue = time
            };

            var action1 = abilityGO.AddComponent<TestAbilityActionAsset>();
            action1.Message = "Lalala";
            var action2 = abilityGO.AddComponent<TestAbilityActionAsset>();
            action2.Message = "Rururu";
            
            timerEventComponentAsset.AddTimerEvent(0.5f, new AbilityActionAssetBase[] { action1, action2 });

            var conversionWorld = CreateConversionWorld();

            throw new System.NotImplementedException();
            //var conversionSystem = conversionWorld.GetOrCreateSystemManaged<GameObjectConversionSystem>();

            var abilityEntity = world.EntityManager.CreateEntity();

            
            //ability.BakeData(abilityEntity, world.EntityManager, conversionSystem);
            
            conversionWorld.Dispose();
            Object.Destroy(ability);
            Object.Destroy(durationComponent);
            Object.Destroy(timerEventComponentAsset);
            Object.Destroy(action1);
            Object.Destroy(action2);
            
            world.EntityManager.SetComponentData(abilityEntity, new AbilityState { Value = AbilityStates.Idle });

            float elapsed = 0;

            while (elapsed < time)
            {
                systemGroup.Update();
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            var counter = world.EntityManager.GetComponentData<TestAbilityActionCounter>(abilityEntity);
            Assert.IsTrue(counter.Value == 2, $"Counter != 2, but {counter.Value}");
        }

        static TestAbilityActionJob createJob(ComponentSystemBase system)
        {
            return new TestAbilityActionJob
            {
                CounterType = system.GetComponentTypeHandle<TestAbilityActionCounter>()
            };
        }
        
        [DisableAutoCreation]
        public partial class TestSystemGroup : ComponentSystemGroup
        {
        }
        
        public static World CreateConversionWorld()
        {
            throw new System.NotImplementedException();
            //var conversionWorld = new World("Conversion world");

            //var systemTypes =  DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.GameObjectConversion);
            //foreach (var systemType in systemTypes)
            //{
            //    conversionWorld.CreateSystem(systemType);
            //}
            //return conversionWorld;
        }

        public struct TestAbilityComponent : IComponentData
        {
            public int Value;
        }

        [BurstCompile]
        public struct AbilityTestJob
        {
            public void OnStarted(ref TestAbilityComponent component)
            {
                component.Value += 1;
            }

            public void Update(float timeDelta, ref TestAbilityComponent component, ref AbilityControl abilityControl)
            {
                component.Value += 1;
                abilityControl.StopRequest = true;
            }

            public void UpdateIdle(float timeDelta, ref TestAbilityComponent component)
            {
                component.Value += 1;
            }

            public void OnStopped(ref TestAbilityComponent component)
            {
                component.Value += 1;
            }
        }
    }
}
