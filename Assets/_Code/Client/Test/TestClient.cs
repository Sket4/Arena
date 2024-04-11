using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Arena.Client;
using TzarGames.MatchFramework.Client;
using System.Threading.Tasks;
using System.Threading;
using Grpc.Core;
using TzarGames.GameCore;
using TzarGames.MatchFramework;

namespace Arena.Tests
{
    class TestAuthTokenProvider : ClientUtility.IAuthTokenProvider
    {
        public string AuthenticationToken { get; set; }
    }

    public class TestClient : MonoBehaviour, IGameInterface
    {
        [SerializeField] string email;
        [SerializeField] string password;

        [SerializeField] private bool setTargetFramerate = false;
        [SerializeField] private int targetFramerate = 60;

        [SerializeField] ServerAddress authServer = new ServerAddress("127.0.0.1", 60600);
        [SerializeField] ServerAddress frontentServerAddress = new ServerAddress("127.0.0.1", 61600);

        [SerializeField] private GameSceneKey safeAreaSceneKey;
        [SerializeField] private GameSceneKey gameSceneKey;
        [SerializeField] private string GameType;

        ClientLauncher launcher;
        BotClientLauncher botClientLauncher;
        CancellationTokenSource cts;

        public GameObject ClientConnectionWindow;
        public TMPro.TMP_InputField EMailInput;

        private Dictionary<string, RoomConnection> roomConnections = new();

        class RoomInfo
        {
            public RoomConnection Connection;
        }

        private IEnumerator Start()
        {
            launcher = GetComponent<ClientLauncher>();
            botClientLauncher = GetComponent<BotClientLauncher>();
            cts = new CancellationTokenSource();

            if (setTargetFramerate)
            {
                Debug.Log($"Setting target framerate to {targetFramerate}");
                Application.targetFrameRate = targetFramerate;
            }
            
            while (launcher.GameLoop == null || launcher.GameLoop.IsLoadingScenes)
            {
                yield return null;
            }

            initGameLoop(launcher.GameLoop);

            EMailInput.text = email;
            EMailInput.onValueChanged.AddListener((val) =>
            {
                this.email = val;
            });
        }

        void initGameLoop(GameLoopBase gameLoop)
        {
            var clientMatchSystem = gameLoop.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
            
            var gameInterfaceEntity = gameLoop.World.EntityManager.CreateEntity();
            gameLoop.World.EntityManager.AddComponentObject(gameInterfaceEntity, new GameInterface(this));
        }

        public void ExitFromMatch()
        {
            launcher.ResetGameLoop();
            initGameLoop(launcher.GameLoop);
                
            RequestSafeZoneGameOnScene(safeAreaSceneKey);
        }

        private void OnDestroy()
        {
            cts.Cancel();
        }

        private void Update()
        {
            if (launcher.GameLoop == null)
            {
                return;
            }

            var gameLoop = launcher.GameLoop as GameClient;

            if (gameLoop == null || gameLoop.ClientSystem == null)
            {
                return;
            }
            
            var isConnected = gameLoop.ClientSystem.IsConnected;

            if (ClientConnectionWindow.activeSelf)
            {
                if (isConnected)
                {
                    ClientConnectionWindow.SetActive(false);
                }
            }
            else
            {
                if (isConnected == false)
                {
                    ClientConnectionWindow.SetActive(true);
                }
            }
        }

        [ContextMenu("Request single player for bot")]
        public void RequestSinglePlayerFotBot(bool safeArea)
        {
            var botClient = botClientLauncher.CreateBotClient("Bot1");
            requestOnlineGame(safeArea, false, "bot1@tzargames.com", "BotPassword123", GameType, gameSceneKey.Id, botClient);
        }
        
        [ContextMenu("Request multiplayer player for bot")]
        public void RequestMultiPlayerFotBot(bool safeArea)
        {
            var botClient = botClientLauncher.CreateBotClient("Bot1");
            requestOnlineGame(safeArea, true, "bot1@tzargames.com", "BotPassword123", GameType, gameSceneKey.Id, botClient);
        }

        [ContextMenu("Request single player")]
        public void RequestSinglePlayer()
        {
            RequestSinglePlayerByGameTypeName(GameType);
        }

        public void RequestMultiplayer()
        {
            RequestMultiplayerByGameTypeName(GameType);
        }

        public void RequestSinglePlayerByGameTypeName(string gameTypeName)
        {
            requestOnlineGame(false, false, email, password, gameTypeName, gameSceneKey.Id, launcher.GameLoop as GameClient, true);
        }

        public void RequestMultiplayerByGameTypeName(string gameTypeName)
        {
            requestOnlineGame(false, true, email, password, gameTypeName, gameSceneKey.Id, launcher.GameLoop as GameClient, true);
        }

        public void RequestSinglePlayerGameOnScene(GameSceneKey scene)
        {
            requestOnlineGame(false, false, email, password, "ArenaMatch", scene.Id, launcher.GameLoop as GameClient, true);
        }
        
        public void RequestSafeZoneGameOnScene(GameSceneKey scene)   
        {
            requestOnlineGame(true, false, email, password, "Town_1", scene.Id, launcher.GameLoop as GameClient, true);
        }

        [ContextMenu("Request single player")]
        public async Task<bool> requestOnlineGame(bool safeAreaConnection, bool isMultiplayer, string email, string password, string gameType, int gameSceneKey, GameClient gameClient, bool log = false)
        {
            if (Application.isEditor)
            {
                email = "e." + email;
            }
            
            if(log) Debug.Log($"Requesting single player game for {email}");
            var cancellationToken = cts.Token;
            
            if(log) Debug.Log("waiting for scenes...");

            while (gameClient.IsLoadingScenes)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                await Task.Yield();
            }
            
            if(log) Debug.Log("finished loading scenes, authenticating...");

            var authToken = await authenticateUser(false, email, password, cancellationToken);
            
            var tokenProvider = new TestAuthTokenProvider();
            tokenProvider.AuthenticationToken = authToken;

            var client = ClientGameUtility.GetGameClient(tokenProvider, frontentServerAddress.Address, frontentServerAddress.Port, frontentServerAddress.Certificate.text);

            try
            {
                if(log) Debug.Log("auth finished, get selected character...");
                var selectedCharacter = await client.Service.GetSelectedCharacterAsync(new GetSelectedCharacterRequest(), cancellationToken: cancellationToken);

                if(log) Debug.Log($"get selected character finished, result: {selectedCharacter}");
                
                if(selectedCharacter.Character == null)
                {
                    Debug.Log("no selected character");

                    var getCharsRequest = new GetCharactersRequest();
                    var charactersResult = await client.Service.GetCharactersAsync(getCharsRequest);

                    if (charactersResult.Characters == null || charactersResult.Characters.Count == 0)
                    {
                        Debug.Log($"no characters for user {email}, creating one");
                        var createRequest = new CreateCharacterRequest()
                        {
                            Class = (int)CharacterClass.Knight,
                            Name = email
                        };
                        var createResult = await client.Service.CreateCharacterAsync(createRequest, cancellationToken: cancellationToken);
                        selectedCharacter.Character = createResult.Character;
                        Debug.LogFormat("Created character {0} xp {1}", createResult.Character.Class, createResult.Character.XP);
                    }
                    else
                    {
                        selectedCharacter.Character = charactersResult.Characters[0];
                    }

                    Debug.Log("Selecting character");
                    await client.Service.SelectCharacterAsync(new SelectCharacterRequest { Name = selectedCharacter.Character.Name });
                }
                
                if(log) Debug.Log($"Starting room connection...");

                AsyncDuplexStreamingCall<ClientRoomMessage, ServerRoomMessage> roomCall;

                if (safeAreaConnection)
                {
                    roomCall = client.Service.SafeAreaRoomConnection(cancellationToken: cancellationToken);
                }
                else
                {
                    roomCall = client.Service.GameRoomConnection(cancellationToken: cancellationToken);
                }
                var roomConnection = new RoomConnection(roomCall.RequestStream, roomCall.ResponseStream);

                while (roomConnections.ContainsKey(email))
                {
                    await Task.Delay(100);
                }
                roomConnections.Add(email, roomConnection);

                var gameRequestMessage = RoomMessages.CreateGameRequestMessage(gameType);
                gameRequestMessage.MetaData.IntKeyValues.Add(MetaDataKeys.SceneId, gameSceneKey);
                
                if (isMultiplayer)
                {
                    gameRequestMessage.MetaData.BoolKeyValues.Add(MetaDataKeys.MultiplayerGame, true);    
                }
                
                
                _ = roomConnection.WriteRequest(gameRequestMessage);

                if(log) Debug.Log($"Handle room connections...");
                
                var endMessage = await roomConnection.HandleRoomMessages((message) => true, cancellationToken);
                roomConnection.Close();

                roomConnections.Remove(email);

                Debug.Log("Client room connection finished");

                if (endMessage.IsSuccess() == false)
                {
                    Debug.Log($"Single player request failed, {endMessage.MetaData.DumpDataToString()}");
                    return false;
                }

                Debug.Log($"Single player request success, {endMessage.MetaData.DumpDataToString()}");

                var keyData = endMessage.GetEncryptionKey();
                var key = SymmetricEncryptionKey.CreateFromBytes(keyData);

                var authSystem = gameClient.World.GetExistingSystemManaged<ClientAuthenticationSystem>();
                authSystem.EncryptionKey = key;

                var authService = authSystem.AuthenticationService as ClientAuthenticationService;
                authService.AuthenticationToken = authToken;

                gameClient.ConnectToServer(endMessage.GetGameServerHost(), (ushort)endMessage.GetGameServerPort());
                
                return true;
            }
            finally
            {
                _ = client.ShutdownAsync();
                roomConnections.Remove(email);
            }
        }

        async Task<string> authenticateUser(bool useAnonymAuth, string playerEmail, string playerPassword, CancellationToken cancellationToken = default)
        {
            Authentication.AuthServerIp = authServer.Address;
            Authentication.AuthServerPort = authServer.Port;

            if (useAnonymAuth)
            {
                var firebaseToken = await Authentication.FirebaseSignInAnonymously();
                return await Authentication.AuthenticateUsingFirebaseToken(firebaseToken, authServer.Certificate.text);
            }
            else
            {
                var firebaseToken = await Authentication.FirebaseSignInByEmailAndPassword(playerEmail, playerPassword);
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    Debug.Log($"Failed to authenticate with email {playerEmail}, trying to create account");
                    bool createAccResult = await Authentication.FirebaseCreateUserWithEmailAndPassword(playerEmail, playerPassword);
                    Debug.Log($"player {playerEmail} acc creation result: {createAccResult}");
                    firebaseToken = await Authentication.FirebaseSignInByEmailAndPassword(playerEmail, playerPassword);
                }

                Debug.Log($"player {playerEmail} for token {firebaseToken}");

                return await Authentication.AuthenticateUsingFirebaseToken(firebaseToken, authServer.Certificate.text);
            }
        }

        public Task<bool> StartQuest(QuestGameInfo questGameInfo)
        {
            launcher.ResetGameLoop();
            initGameLoop(launcher.GameLoop);
            
            return requestOnlineGame(false, questGameInfo.Multiplayer, email, password, questGameInfo.MatchType, questGameInfo.GameSceneID.Value, launcher.GameLoop as GameClient);
        }
    }
}
