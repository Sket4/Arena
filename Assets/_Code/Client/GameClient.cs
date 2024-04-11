using Unity.Entities;
using UnityEngine;
using TzarGames.MultiplayerKit;
using TzarGames.MultiplayerKit.Client;
using TzarGames.GameFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Client.Abilities;
using TzarGames.GameCore.Client;
using Unity.CharacterController;

namespace Arena.Client
{
    public class GameClient : TzarGames.MatchFramework.Client.GameClientLoopBase
    {
        PresentationSystemGroup presentationSystemGroup;

        public string AuthToken { get; set; }

        public bool PendingDestroy
        {
            get; set;
        }
        public bool IsBot { get; private set; }

        // TODO use simulator
        public GameClient(string name, bool bot, ClientGameSettings gameSettings, Unity.Entities.Hash128[] additionalScenes, bool useDebugSiconnectTimeout) 
            : base(name, false, default, useDebugSiconnectTimeout, gameSettings.EnableDebugJournaling, gameSettings.MaxDebugJournalRecordCount)
        {
            InitSceneLoading(additionalScenes);

            IsBot = bot;

            if(Instantiator.IsInitialized == false)
            {
                Object.Instantiate(gameSettings.InstantiatorPrefab);
            }

            Utils.AddSharedSystems(this, false, "Client");

            if(IsBot == false)
            {
                presentationSystemGroup = World.GetOrCreateSystemManaged<PresentationSystemGroup>();
            }
            
            GameLoopUtils.AddSystems(this, bot, false, gameSettings);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if(IsBot == false)
            {
                presentationSystemGroup.Update();
            }
        }
    }
}
