using System.Collections;
using System.Collections.Generic;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using TzarGames.MultiplayerKit.Server;
using TzarGames.MultiplayerKit.Tests;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TzarGames.GameCore.Tests
{
    [AlwaysUpdateSystem, DisableAutoCreation]
partial     class MovementInputTestSystem : SystemBase
    {
        public bool LocalControl;

        protected override void OnUpdate()
        {
            var move = new float3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            move = math.normalizesafe(move);
            var seed = math.max((uint) UnityEngine.Random.Range(int.MinValue, int.MaxValue), 1);
            var localControl = LocalControl;

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref CharacterInputs movement, ref MultiplayerKit.NetworkPlayer player) =>
            {
                if (localControl)
                {
                    if (player.ItsMe)
                    {
                        movement.MoveVector = move;
                    }
                }
                else
                {
                    if (player.ItsMe)
                    {
                        var rand = new Unity.Mathematics.Random(seed + (uint)entityInQueryIndex);

                        movement.MoveVector = new float3(rand.NextFloat(-1, 1), 0, rand.NextFloat(-1, 1));
                        movement.MoveVector = math.normalizesafe(movement.MoveVector);
                    }
                }
            }).Run();
        }
    } 

    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    partial class TestSpawnSystem : SystemBase, IRpcProcessor
    {
        public NetworkIdentity NetIdentity { get; set; }
        public GameObject Character;
        bool botMode = false;

        struct SpawnInfo
        {
            public Entity Entity;
            public MultiplayerKit.NetworkPlayer Player;
            public NetworkID Id;
            public bool Bot;
        }

        List<SpawnInfo> pendingSpawn = new List<SpawnInfo>();

        protected override void OnUpdate()
        {
            foreach (var sp in pendingSpawn)
            {
                Entity entity = Entity.Null;

                if (sp.Entity == Entity.Null)
                {
                    entity = World.EntityManager.CreateEntity();
                }
                else
                {
                    entity = sp.Entity;
                }

                spawnInternal(entity, sp.Player, sp.Id, sp.Bot, World.EntityManager);
            }

            pendingSpawn.Clear();

            if (NetIdentity.Net.IsServer)
            {
                var commandBuffer = World.GetExistingSystemManaged<Unity.Entities.EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
                var allocator = World.GetOrCreateSystemManaged<NetworkIdentitySystem>().Allocator;

                Entities
                    .WithoutBurst()
                    .WithStructuralChanges()
                    .WithAll<MultiplayerKit.NetworkPlayer>().WithNone<Player>().ForEach((Entity entity, ref MultiplayerKit.NetworkPlayer networkPlayer) =>
                {
                    commandBuffer.AddComponent(entity, new Player());

                    var id = new NetworkID(allocator.AllocateId());

                    this.RPC(Spawn, networkPlayer.ID, id);

                    var spawnInfo = new SpawnInfo()
                    {
                        Entity = entity,
                        Id = id,
                        Player = MultiplayerKit.NetworkPlayer.Null,
                        Bot = true
                    };
                    pendingSpawn.Add(spawnInfo);
                }).Run();
            }
        }

        public void SetBotMode(bool bot)
        {
            botMode = bot;
        }

        [RemoteCall]
        public void Spawn(int networkPlayerID, NetworkID id)
        {
            bool itsMe = World.GetExistingSystemManaged<ClientSystem>().ConnectionID == networkPlayerID;
            var spawnInfo = new SpawnInfo()
            {
                Entity = Entity.Null,
                Id = id,
                Player = new MultiplayerKit.NetworkPlayer(networkPlayerID, itsMe),
                Bot = !itsMe || botMode
            };
            pendingSpawn.Add(spawnInfo);
        }

        void spawnInternal(Entity entity, MultiplayerKit.NetworkPlayer networkPlayer, NetworkID id, bool bot, EntityManager manager)
        {
            Debug.LogFormat("SpawnRPC for player {0} netID: {1}, entity: {2}, world: {3}", networkPlayer, id, entity, World.Name);

            try
            {
                if (networkPlayer.IsValid)
                {
                    manager.AddComponentData(entity, networkPlayer);
                }

                manager.AddComponentData(entity, id);
                var instance = Object.Instantiate(Character);
                instance.name = string.Format("{0}, bot:{1}, entity:{2}", manager.World.Name, bot, entity);
                Debug.LogError("Not implemented");
                //Utility.AddGameObjectToEntity(instance, manager, entity);

                if (bot)
                {
                    instance.GetComponentInChildren<Camera>().enabled = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("World " + World.Name);
                Debug.LogException(ex);
            }
        }
    }

    public class MovementSystemTest : MonoBehaviour
    {
        public GameObject Character;
        public GameObject Bot;

        public Transform PathTarget;

        public int BotCount = 100;
        public Transform MinCorner;
        public Transform MaxCorner;

        List<Entity> clientCharacterEntities = new List<Entity>();
        List<MultiplayerKit.Tests.ClientTester> clients = new List<MultiplayerKit.Tests.ClientTester>();
        MultiplayerKit.Tests.ServerTester server;
        TesterFactory TesterFactory = new TesterFactory();

        void Start()
        {
            // SERVER
            {
                server = TesterFactory.CreateServerTester("server");

                var identity = server.CreateIdentity(new NetworkID { ID = 1 });
                
                server.CreateSystemManaged<GameCommandBufferSystem>();
                server.CreateSystemManaged<TimeSystem>(server.Net);

                var spawnSystem = server.CreateSystemManaged<TestSpawnSystem>();
                spawnSystem.NetIdentity = identity;
                identity.RegisterNetworkObject(spawnSystem);
                spawnSystem.Character = Character;

                var pathSystem = server.CreateSystemManaged<PathMovementSystem>(false);
                var system = server.CreateSystemManaged<NavmeshMovementSystem>(false);
                
                Debug.LogError("var syncSystem = server.CreateSystemManaged<Server.MovementNetSyncSystem>(false);");
                //var syncSystem = server.CreateSystemManaged<Server.MovementNetSyncSystem>(false);
                //syncSystem.NetIdentity = identity;
                //identity.RegisterNetworkObject(syncSystem);


                var dataSystem = server.CreateSystemManaged<MultiplayerKit.Server.DataSyncSystem>(identity.Net);
                Debug.LogError("MovementSync");
                //dataSystem.AddDataSync(new MovementSync());

                

                server.Start();
            }

            // CLIENT
            {
                StartCoroutine(createClient("client 1", true));
                StartCoroutine(createClient("client 2", false));
            }


            //Character.SetActive(false);
        }

        IEnumerator createClient(string name, bool player)
        {
            var client = TesterFactory.CreateClientTester(name);
            clients.Add(client);

            var identity = client.CreateIdentity(new NetworkID { ID = 1 });
            
            client.CreateSystemManaged<GameCommandBufferSystem>();
            client.CreateSystemManaged<TimeSystem>(client.Net);
            var spawnSystem = client.CreateSystemManaged<TestSpawnSystem>();
            spawnSystem.NetIdentity = identity;
            identity.RegisterNetworkObject(spawnSystem);
            spawnSystem.Character = Character;

            var syncSystem = client.CreateSystemManaged<Client.MovementNetSyncSystem>(false);
            var dataSystem = client.CreateSystemManaged<MultiplayerKit.Client.DataSyncSystem>(identity.Net);
            Debug.LogError("MovementSync");
            //dataSystem.AddDataSync(new MovementSync());
            var pathSystem = client.CreateSystemManaged<PathMovementSystem>(false);
            var system = client.CreateSystemManaged<NavmeshMovementSystem>();
            var postSyncSystem = client.CreateSystemManaged<Client.PostMovementNetSyncSystem>();

            syncSystem.NetIdentity = identity;
            postSyncSystem.NetIdentity = identity;
            identity.RegisterNetworkObject(postSyncSystem);
            identity.RegisterNetworkObject(syncSystem);

            var min = MinCorner.position;
            var max = MaxCorner.position;

            for (int i = 0; i < BotCount; i++)
            {
                var botEntity = client.MyWorld.EntityManager.CreateEntity();
                var bot = Instantiate(Bot);

                bot.transform.position = new Vector3(UnityEngine.Random.Range(min.x, max.x), 0, UnityEngine.Random.Range(min.z, max.z));
                Debug.LogError("not implemented");
                //Utility.AddGameObjectToEntity(bot, client.MyWorld.EntityManager, botEntity);
            }

            var inputSystem = client.CreateSystemManaged<MovementInputTestSystem>();

            if (player)
            {
                inputSystem.LocalControl = true;
                spawnSystem.SetBotMode(false);
            }
            else
            {
                spawnSystem.SetBotMode(true);
            }

            client.Connect();

            while (client.Net.IsConnected == false)
            {
                yield return null;
            }
        }

        void Update()
        {
            TesterFactory.Update();
        }

        [ContextMenu("Test path")]
        void TestPath()
        {
            var pos = PathTarget.position;

            clients[0].MyWorld.EntityManager.SetComponentData(clientCharacterEntities[0], new PathMovement()
            {
                MoveToTargetPosition = true,
                PathCalculated = false,
                TargetPosition = PathTarget.position
            });
        }
    }
}
