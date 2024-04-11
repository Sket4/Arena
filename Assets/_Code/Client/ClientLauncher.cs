using System.Collections;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Client;
using UnityEngine;

namespace Arena.Client
{
    public class ClientLauncher : BaseClientGameLauncher
    {
        [SerializeField] private ClientGameSettings settings = default;
        [SerializeField] bool autoConnect = true;
        [SerializeField] bool useDebugDisconnectTimeout = false;

        protected override void Start()
        {
            base.Start();
            
            if(autoConnect)
            {
                StartCoroutine(autoConnectRoutine());
            }
        }

        IEnumerator autoConnectRoutine()
        {
            while(GameLoop.IsLoadingScenes)
            {
                yield return null;
            }
            Debug.Log("Finished loading scenes, autoconnecting...");

            var gameState = GameState.Instance;

            if (gameState != null)
            {
                var gameInterfaceEntity = GameLoop.World.EntityManager.CreateEntity();
                GameLoop.World.EntityManager.AddComponentObject(gameInterfaceEntity, new GameInterface(gameState));
            }

            var onlineGameInfo = GameState.GetOnlineGameInfo();
            ConnectToServer(gameState.AuthenticationToken, onlineGameInfo.ServerHost, (ushort)onlineGameInfo.ServerPort, onlineGameInfo.EncryptionKey);
        }

        public void ConnectToServer(string authToken, string serverAddress, ushort serverPort, SymmetricEncryptionKey encryptionKey)
        {
            var clientGameLoop = GameLoop as GameClient;

            var authSystem = clientGameLoop.World.GetExistingSystemManaged<ClientAuthenticationSystem>();
            authSystem.EncryptionKey = encryptionKey;

            var authService = authSystem.AuthenticationService as ClientAuthenticationService;
            authService.AuthenticationToken = authToken;

            clientGameLoop.ConnectToServer(serverAddress, (ushort)serverPort);
        }

        protected override GameLoopBase CreateGameLoop(Unity.Entities.Hash128[] additionalScenes)
        {
            var clientGameLoop = new GameClient("Client", false, settings, additionalScenes, useDebugDisconnectTimeout);
            return clientGameLoop;
        }

        public void RestartAndReconnect()
        {
            ResetGameLoop();
            
            StopAllCoroutines();

            StartCoroutine(autoConnectRoutine());
        }
    }
}
