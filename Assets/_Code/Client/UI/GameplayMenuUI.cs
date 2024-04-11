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
        [SerializeField] private UIBase pauseMenuForLobby = default;
        [SerializeField] private GameObject exitToMainMenuButton;

        private bool isPendingShow = true;
        
        public void ExitToMainMenu()
        {
            GameState.Instance.ExitToMainMenu();
            pauseMenuForGame.SetVisible(false);
            pauseMenuForLobby.SetVisible(false);
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
                    pauseMenuForGame.SetVisible(true);
                    pauseMenuForLobby.SetVisible(false);
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
