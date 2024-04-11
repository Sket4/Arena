using System.Collections.Generic;
using UnityEngine;
using TzarGames.MatchFramework;
using System.Threading.Tasks;
using Unity.Entities;

namespace Arena.Client
{
    public class BotClientLauncher : MonoBehaviour
    {
        class ClientInfo
        {
            public GameClient Game;
        }

        [SerializeField]
        Unity.Scenes.SubScene[] additionalScenes;

        [SerializeField]
        bool useSimulator = false;

        [SerializeField]
        SimParameters simParameters;

        [SerializeField]
        ClientGameSettings gameSettings;

        List<ClientInfo> clientLoops = new List<ClientInfo>();

        public async Task DestroyGame(GameClient gameLoop)
        {
            var info = getInfoByGameLoop(gameLoop);
            if(info != null)
            {
                clientLoops.Remove(info);
            }
            gameLoop.Disconnect();
            await Task.Yield();
            gameLoop.Dispose();
        }

        ClientInfo getInfoByGameLoop(GameClient gameLoop)
        {
            foreach(var client in clientLoops)
            {
                if(client.Game == gameLoop)
                {
                    return client;
                }    
            }
            return null;
        }

        public GameClient CreateBotClient(string gameName, bool lightweight = false)
        {
            var additionalSceneHashes = new Unity.Entities.Hash128[additionalScenes.Length];
            for (int i = 0; i < additionalScenes.Length; i++)
            {
                var scene = additionalScenes[i];
                additionalSceneHashes[i] = scene.SceneGUID;
            }

            var clientLoop = new GameClient(gameName, true, gameSettings, additionalSceneHashes, useSimulator);

            //if(lightweight)
            {
                //clientLoop.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Enabled = false;
                //clientLoop.World.GetExistingSystemManaged<TzarGames.GameCore.Abilities.AbilitySystem>().Enabled = false;
                clientLoop.World.GetExistingSystemManaged<PresentationSystemGroup>().Enabled = false;

                //clientLoop.World.GetExistingSystemManaged<TzarGames.GameCore.CharacterControllerSystem>().Enabled = false;
            }

            clientLoops.Add(new ClientInfo
            {
                Game = clientLoop,
            });
            return clientLoop;
        }

        private void Update()
        {
            foreach(var client in clientLoops)
            {
                client.Game.Update();
            }
        }

        private void OnDestroy()
        {
            foreach(var client in clientLoops)
            {
                client.Game.Dispose();
            }
        }
    }
}
