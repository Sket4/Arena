using System.Collections;
using UnityEngine;
using Unity.Entities;
using TzarGames.MultiplayerKit;
using Arena.Server;
using TzarGames.GameCore.Tests;
using TzarGames.GameCore;
using System.Threading.Tasks;
using Unity.Collections;
using System;
using UnityEngine.Serialization;
using TzarGames.MatchFramework.Server;
using TzarGames.MatchFramework.Database;
using TzarGames.MatchFramework;
using System.Threading;

namespace Arena.Tests
{
    public class TestServerGameLauncher : MonoBehaviour, IServerLauncher
    {
        [SerializeField]
        int fps = 20;
        
        [SerializeField, FormerlySerializedAs("testGameType")]
        GameType gameType = default;

        [SerializeField]
        MatchService matchServer = default;

        [SerializeField] private Unity.Scenes.SubScene[] additionalScenes;

        [SerializeField]
        bool drawDebugInfo = default;

        [SerializeField]
        float debugOffset = 400;

        ServerTester server;

        [SerializeField]
        GameObject[] sceneNetObjects = default;

        TesterFactory factory;

        const string launchTestSaveKey = "ARENA_TEST_LAUNCHSERVER";
        const string launchMatchSaveKey = "ARENA_TEST_LAUNCHMATCHSERVER";

        ushort port = 9000;

        public static bool LaunchTestServer
        {
            get
            {
#if UNITY_EDITOR
                return PlayerPrefs.GetInt(launchTestSaveKey, 1) == 1;
#else
                return false;
#endif
            }
            set
            {
                PlayerPrefs.SetInt(launchTestSaveKey, value ? 1 : 0);
            }
        }
        
        public static bool LaunchMatchServer
        {
            get
            {
#if UNITY_EDITOR
                return PlayerPrefs.GetInt(launchMatchSaveKey, 1) == 1;
#else
                return false;
#endif
            }
            set
            {
                PlayerPrefs.SetInt(launchMatchSaveKey, value ? 1 : 0);
            }
        }
        
        public Invoker Invoker { get { return factory.Invoker; } }

        void Awake()
        {
            factory = new TesterFactory();

            if (LaunchMatchServer == false && matchServer != null)
            {
                matchServer.enabled = false;
            }
            
            if (LaunchTestServer)
            {
                if (matchServer != null)
                {
                    matchServer.enabled = false;
                }
            }
        }

        async Task Start()
        {
            foreach (var nobj in sceneNetObjects)
            {
                nobj.gameObject.SetActive(false);
            }

            StartCoroutine(update());

            if (LaunchTestServer)
            {
                await CreateTestServer();
            }
        }

        [ContextMenu("Уничтожить все сервера")]
        public void DestroyAllServers()
        {
            factory.DestroyAll();
        }

        [ContextMenu("Создать тестовый сервер")]
        public async Task CreateTestServer()
        {
            Debug.Log("Launching test game server");

            gameType.DebugMode = true;
            await gameType.Initialize(this);

            var request = new ServerGameRequest();
            request.UserRequests.Add(new UserRequest { UserId = new AccountId { Value = 1 } });

            try
            {
                var gameServerResult = await gameType.HandleGameRequest(request);

                server = factory.GetTesterFromGameServer(gameServerResult.GameServer as GameServerLoop) as ServerTester;

                foreach (var nobj in sceneNetObjects)
                {
                    var instance = Instantiate(nobj);
                    instance.SetActive(true);
                    instance.name = nobj.name + " (server)";
                    var id = instance.GetComponent<NetworkIdComponent>().Value;
                    server.CreateNetworkObject(id, out NetworkIdentity networkIdentity, out Entity sceneEntity);
                    Debug.LogError("not implemented");
                    //Utility.AddGameObjectToEntity(instance, server.World.EntityManager, sceneEntity);
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Material debugMetarial;

        void OnGUI()
        {
            var servers = factory.GetServerTesters();

            if (servers == null || servers.Count == 0)
            {
                return;
            }

            Rect currentRect = new Rect(0, debugOffset, 700, 400);
            
            var serverSystem = servers[0].ServerSystem;

            if (drawDebugInfo)
            {
                var debugData = serverSystem.GetDebugInfo();

                var (keys, length) = debugData.GetUniqueKeyArray(Allocator.Temp);

                for (int k = 0; k < length; k++)
                {
                    var key = keys[k];

                    double latestTime = double.MinValue;
                    PacketDebugInfo latestPacket = default;

                    if (debugData.TryGetFirstValue(key, out PacketDebugInfo info, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        if (info.CreationTime > latestTime)
                        {
                            latestTime = info.CreationTime;
                            latestPacket = info;
                        }

                        while (debugData.TryGetNextValue(out info, ref iterator))
                        {
                            if (info.CreationTime > latestTime)
                            {
                                latestTime = info.CreationTime;
                                latestPacket = info;
                            }
                        }
                    }

                    GUI.Box(currentRect, "");

                    var newRect = currentRect;
                    newRect.x += 10;

                    GUILayout.BeginArea(newRect);

                    GUILayout.Label("Connection: " + key);
                    GUILayout.Label("Latest packet size: " + latestPacket.Size);

                    if (Event.current.type == EventType.Repaint)
                    {
                        GUI.color = Color.green;

                        if(debugMetarial == null)
                        {
                            debugMetarial = new Material(Shader.Find("UI/Default"));
                            debugMetarial.hideFlags = HideFlags.HideAndDontSave;
                        }

                        float width = 600;
                        float height = 300;

                        var rect = GUILayoutUtility.GetLastRect();
                        var top = new Vector2(rect.min.x, rect.max.y + 10);
                        var rBottom = top;
                        rBottom.x += width;
                        rBottom.y += height;
                        
                        int maxBytes = 500;
                        float maxTime = 5;

                        debugMetarial.SetPass(0);

                        GL.PushMatrix();
                        GL.Begin(GL.LINES);

                        GL.Color(Color.green);

                        // rect
                        GL.Vertex3(top.x, top.y, 0);
                        GL.Vertex3(top.x + width, top.y, 0);

                        GL.Vertex3(top.x + width, top.y, 0);
                        GL.Vertex3(top.x + width, top.y + height, 0);

                        GL.Vertex3(top.x + width, top.y + height, 0);
                        GL.Vertex3(top.x, top.y + height, 0);

                        GL.Vertex3(top.x, top.y + height, 0);
                        GL.Vertex3(top.x, top.y, 0);

                        var dottedColor = Color.green;
                        dottedColor.a = 0.25f;
                        GL.Color(dottedColor);

                        int vertGuideCount = 5;
                        float currentGuideY = height / vertGuideCount;

                        for(int d=0; d<vertGuideCount-1; d++)
                        {
                            currentGuideY += height / vertGuideCount;
                            
                            GL.Vertex3(top.x, currentGuideY, 0);
                            GL.Vertex3(top.x + width, currentGuideY, 0);
                        }

                        // packets
                        GL.Color(new Color(1,1,0));

                        var currTime = serverSystem.NetTime;

                        if (debugData.TryGetFirstValue(key, out info, out iterator))
                        {
                            var timeDiff = currTime - info.CreationTime;

                            var start = new Vector3(rBottom.x - (float)timeDiff / maxTime * width, rBottom.y - ((float)info.Size / maxBytes) * height, 0);
                            GL.Vertex(start);
                            start.x -= 5.0f;
                            GL.Vertex(start);

                            while (debugData.TryGetNextValue(out info, ref iterator))
                            {
                                timeDiff = currTime - info.CreationTime;

                                start = new Vector3(rBottom.x - (float)timeDiff / maxTime * width, rBottom.y - ((float)info.Size / maxBytes) * height, 0);
                                GL.Vertex(start);
                                start.x -= 5.0f;
                                GL.Vertex(start);
                            }
                        }

                        GL.End();
                        GL.PopMatrix();

                        GUI.Label(new Rect(top.x + width + 10, top.y, 100, 25), string.Format("{0} байт", maxBytes));
                        GUI.Label(new Rect(top.x + width + 10, top.y + height, 100, 25), string.Format("0 байт", maxBytes));
                    }

                    GUILayout.EndArea();

                    currentRect.y += currentRect.height;
                }

                keys.Dispose();
            }
        }

        static void drawDottedLine(Vector3 start, Vector3 end)
        {
            float size = 10.0f;

            var magn = Vector3.Distance(start, end);
            var dir = (end - start).normalized;
            var cnt = (int)(magn / size);

            float dist = cnt % 2 > 0 ? 0 : size;

            for(int i=0; i<cnt; i+=2)
            {
                dist = i * 10.0f;

                GL.Vertex(start + dir * dist);
                GL.Vertex(start + dir * (dist + 10.0f));
            }
        }

        IEnumerator update()
        {
            while(true)
            {
                if(enabled == false)
                {
                    yield return null;
                    continue;
                }

                Invoker.RunPendingActions();

                factory.Update();
                yield return new WaitForSeconds(1.0f / fps);
            }
        }

        public void SetPortRange(ushort min, ushort max)
        {
            //
        }

        public IGameServer CreateServer(IServerGameSettings gameSettings)
        {
            var addScenes = new Unity.Entities.Hash128[additionalScenes.Length];
            for (int i = 0; i < additionalScenes.Length; i++)
            {
                var scene = additionalScenes[i];
                addScenes[i] = scene.SceneGUID;
            }

            ServerGameSettings settingsAsset = (gameSettings is ServerGameSettings ? (gameSettings as ServerGameSettings) : null);
            var tester = factory.CreateServerTester($"{gameSettings.ServerName} ({port})", settingsAsset, addScenes);
            tester.DefaultPort = port;
            tester.Start();
            port++;
            return tester.Server;
        }

        private void OnDestroy()
        {
            if(factory != null)
            {
                factory.DestroyAll();
            }

            if(debugMetarial != null)
            {
                Destroy(debugMetarial);
            }
        }

        public float GetLoad()
        {
            return 0;
        }

        public Task<GameSessionStatusResult> GetGameSessionStatus(string gameSessionId, PlayerId[] playerIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //foreach(var game in factory.GetServerTesters())
            //{
            //    game.Server.
            //}
        }
    }
}
