// Copyright 2012-2024 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using Arena.Client;
using Arena.Server;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using UnityEngine;

namespace Arena.Client.UI
{
    public class GameplayMenuUI : TzarGames.GameFramework.UI.GameUIBase
    {
        [SerializeField] private UIBase pauseMenuForGame = default;
        [SerializeField] private UIBase exitMenuForGame = default;
        [SerializeField] private UIBase pauseMenuForLobby = default;
        [SerializeField] private UIBase settingsMenu;
        [SerializeField] private GameObject exitToMainMenuButton;

        private bool isPendingShow = true;

        public bool ExitMode { get; set; }

        public void ExitToMainMenu()
        {
            GameState.Instance.ExitToMainMenu();
            pauseMenuForGame.SetVisible(false);
            pauseMenuForLobby.SetVisible(false);
            exitMenuForGame.SetVisible(false);
        }

        public void ShowSettings(bool show)
        {
            settingsMenu.SetVisible(show);

            if (show)
            {
                pauseMenuForGame.SetVisible(false);
                pauseMenuForLobby.SetVisible(false);
                exitMenuForGame.SetVisible(false);
            }
            else
            {
                isPendingShow = true;
            }
        }

        public void ReturnToHome()
        {
            exitMenuForGame.SetVisible(false);
            var matchSystem = EntityManager.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
            matchSystem.NotifyExitFromGame(true, GetData<PlayerController>().Value);
        }

        public override void OnSystemUpdate(UISystem system)
        {
            base.OnSystemUpdate(system);

            if (isPendingShow)
            {
                isPendingShow = false;

                var inSafeZone = system.TryGetSingleton<SafeZoneSyncData>(out _);
                
                if (inSafeZone)
                {
                    pauseMenuForGame.SetVisible(false);
                    pauseMenuForLobby.SetVisible(true);

                    if (GameState.Instance == null)
                    {
                        exitToMainMenuButton.SetActive(false);
                    }
                }
                else
                {
                    if (ExitMode)
                    {
                        ExitMode = false;
                        exitMenuForGame.SetVisible(true);
                        pauseMenuForGame.SetVisible(false);
                        pauseMenuForLobby.SetVisible(false);
                    }
                    else
                    {
                        pauseMenuForGame.SetVisible(true);
                        pauseMenuForLobby.SetVisible(false);
                        exitMenuForGame.SetVisible(false);
                    }
                }
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            isPendingShow = true;
        }
    }
}
