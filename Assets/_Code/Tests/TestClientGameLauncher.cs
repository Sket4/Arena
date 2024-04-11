using System.Collections;
using UnityEngine;
using Unity.Entities;
using TzarGames.MultiplayerKit;
using Arena.Client;
using System.Threading.Tasks;
using TzarGames.GameCore;
using Unity.Transforms;
using TzarGames.MatchFramework.Client;

namespace Arena.Tests
{
    public class TestClientGameLauncher : MonoBehaviour
    {
        [SerializeField]
        ClientGameSettings clientSettings = default;
        
        [SerializeField] private string ip = "127.0.0.1";
        [SerializeField] private ushort port = 9000;

        ClientTester client;
        
        [SerializeField] private Unity.Scenes.SubScene[] additionalScenes;

        [SerializeField]
        GameObject[] sceneNetObjects = default;

        TesterFactory factory;

        const string launchTestSaveKey = "ARENA_TEST_LAUNCHCLIENT";
        const string authTestSaveKey = "ARENA_TEST_AUTHCLIENT";

        [SerializeField]
        GameLocationType testArea = default;

        [SerializeField]
        bool requestMultiplayer = false;

        [SerializeField]
        LogLevel logLevel = LogLevel.Important;

        public GameLocationType TestArea
        {
            get
            {
                return testArea;
            }
        }

        public static bool LaunchTestClient
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

        public static bool AuthorizeTestClient
        {
            get
            {
#if UNITY_EDITOR
                return PlayerPrefs.GetInt(authTestSaveKey, 0) == 1;
#else
                return false;
#endif
            }
            set
            {
                PlayerPrefs.SetInt(authTestSaveKey, value ? 1 : 0);
            }
        }

        async Task Start()
        {
            factory = new TesterFactory();

            foreach (var nobj in sceneNetObjects)
            {
                nobj.gameObject.SetActive(false);
            }
            
            if(LaunchTestClient)
            {
                await createPlayerClient();
            }
            
            StartCoroutine(update());
        }
        

        [ContextMenu("Создать клиент игрока")]
        async Task createPlayerClient()
        {
            await createClient(false);
        }

        readonly string[] characterNames = new string[]
        {
            "Педро",
            "Хулио",
            "Алексеюшка",
            "Иванушка",
            "Динарчик",
            "Айсылу",
            "Зульфат",
            "Руслан",
            "Айдар",
            "Елена",
            "Енот",
            "Айрат",
            "Ильнур",
            "Рыбсик",
            "Пашок",
        };


        string generateCharacterName()
        {
            var name = characterNames[Random.Range(0, characterNames.Length)];
            name = string.Format("{0}{1}", name, Random.Range(0, 9999));
            return name;
        }

        void createLocalClient()
        {
            client = factory.CreateClient(string.Format("Client {0}", generateCharacterName()), false, clientSettings);

            var em = client.World.EntityManager;
            var player = em.CreateEntity();
            em.AddComponentData(player, new TzarGames.MultiplayerKit.NetworkPlayer(0, true, true));
            em.AddComponentData(player, new Player(0, true, true));

            Debug.LogError("Not implemented?");
            // em.AddComponentData(player, new GameObjectSpawnRequest() { PrefabID = clientSettings.PlayerPrefab.Id });
            // em.AddComponentData(player, new Translation() { Value = GameObject.Find("Spawn point").transform.position });
            // em.AddComponentData(player, new Group() { ID = 1 });
        }

        async Task<ClientTester> createClient(bool bot)
        {
            Debug.LogFormat("Запуск клиента, бот == ", bot);

            string token = "";
            string serverIp = ip;
            ushort serverPort = port;

            var playerName = generateCharacterName();

            if (AuthorizeTestClient)
            {
                Authentication.AuthServerIp = ip;

                try
                {
                    if(bot)
                    {
                        var testToken = await ArenaTests.GetTestAuthToken();
                        token = await Authentication.AuthenticateUsingFirebaseToken(testToken, null);
                    }
                    else
                    {
                        var firebaseToken = await Authentication.FirebaseSignInAnonymously();
                        token = await Authentication.AuthenticateUsingFirebaseToken(firebaseToken, null);
                    }

                    Debug.LogError("auth proveder and cert");
                    var gameService = ClientGameUtility.GetGameClient(null, serverIp, serverPort, null);

                    var getCharsRequest = new GetCharactersRequest();
                    var charactersResult = await gameService.Service.GetCharactersAsync(getCharsRequest);
                    CharacterData currentCharacter = null;

                    if (charactersResult.Characters == null || charactersResult.Characters.Count == 0)
                    {
                        var createRequest = new CreateCharacterRequest()
                        {
                            Class = 0,
                            Name = playerName
                        };
                        var createResult = await gameService.Service.CreateCharacterAsync(createRequest);
                        currentCharacter = createResult.Character;
                        Debug.LogFormat("Created character {0} xp {1}", createResult.Character.Class, createResult.Character.XP);
                    }
                    else
                    {
                        currentCharacter = charactersResult.Characters[0];
                    }

                    throw new System.NotImplementedException();

                    //var gameRequest = new GameRequest
                    //{
                    //    GameType = testArea.ToString(),
                    //    Multiplayer = requestMultiplayer
                    //};

                    //var matchGameService = ClientUtility.GetGameService(token, ip); 
                    //var result = await matchGameService.RequestGameAsync(gameRequest);

                    //if (result.Success == false)
                    //{
                    //    Debug.LogError("Request game failed");
                    //    return null;
                    //}

                    //serverIp = result.IP == "127.0.0.1" ? ip : result.IP;
                    //serverPort = (ushort)result.Port;
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return null;
                }
            }
            
            var addScenes = new Unity.Entities.Hash128[additionalScenes.Length];
            for (int i = 0; i < additionalScenes.Length; i++)
            {
                var scene = additionalScenes[i];
                addScenes[i] = scene.SceneGUID;
            }

            client = factory.CreateClient(string.Format("Client {0} ({1})", playerName, bot), bot, clientSettings, addScenes);
            client.ClientSystem.LogLevel = logLevel;
            client.Connect(serverIp, serverPort);

            foreach (var nobj in sceneNetObjects)
            {
                var instance = Instantiate(nobj);
                instance.SetActive(true);
                instance.name = string.Format("{0} ({1})", nobj.name, playerName);
                var id = instance.GetComponent<NetworkIdComponent>().Value;
                client.CreateNetworkObject(id, out NetworkIdentity networkIdentity, out Entity sceneEntity);
                Debug.LogError("not implemented");
                //Utility.AddGameObjectToEntity(instance, client.World.EntityManager, sceneEntity);
            }

            client.Client.AuthToken = token;

            return client;
        }

        [ContextMenu("Создать клиент бота")]
        async Task createBot()
        {
            await createClient(true);
        }

        [ContextMenu("Уничтожить все клиенты")]
        void destroyAll()
        {
            factory.DestroyAll();
        }

        IEnumerator update()
        {
            while(true)
            {
                factory.Update();
                yield return null;
            }
        }

        private void OnDestroy()
        {
            destroy = true;

            if(factory != null)
            {
                factory.DestroyAll();
            }
        }

        bool destroy = false;

        [ContextMenu("Запуск спаун-теста ботов")]
        async Task runBotSpawnTest()
        {
            while(destroy == false)
            {
                if(factory == null)
                {
                    break;
                }
                var client = await createClient(true);
                await Task.Delay(UnityEngine.Random.Range(0, 5000));
                factory.Destroy(client);
            }
        }

        [ContextMenu("Запуск спаун-теста игрока")]
        async Task runPlayerSpawnTest()
        {
            while (destroy == false)
            {
                if (factory == null)
                {
                    break;
                }
                var client = await createClient(false);
                await Task.Delay(UnityEngine.Random.Range(0, 5000));
                factory.Destroy(client);
            }
        }
    }
}
