using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameFramework.UI;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class MatchStateUI : GameUIBase
    {
        public Button ExitFromGameButton;
        public Button ContinueGameButton;
        public Button RestartGameButton;
        public TextUI TimerCounter;
        public TextUI WaitingOthersText;

        public UIBase DecisionWindow;
        public UIBase ConfirmExitWindow;
        public UIBase ExitingWindow;
        public UIBase LoadingWindow;

        private bool isExiting = false;

        protected override void OnVisible()
        {
            base.OnVisible();
            ShowDecisionWindow();

            LoadingWindow.SetVisible(false);
            ConfirmExitWindow.SetVisible(false);
            ExitingWindow.SetVisible(false);
        }

        public override void OnSystemUpdate(UISystem system)
        {
            base.OnSystemUpdate(system);

            if(system.HasSingleton<ArenaMatchStateData>() == false)
            {
                return;
            }

            var arenaState = system.GetSingleton<ArenaMatchStateData>();

            if(arenaState.IsNextSceneAvailable)
            {
                if(ContinueGameButton.gameObject.activeSelf == false)
                {
                    ContinueGameButton.gameObject.SetActive(true);
                }
                if (RestartGameButton.gameObject.activeSelf)
                {
                    RestartGameButton.gameObject.SetActive(false);
                }
            }
            else
            {
                if (ContinueGameButton.gameObject.activeSelf)
                {
                    ContinueGameButton.gameObject.SetActive(false);
                }
                if (RestartGameButton.gameObject.activeSelf == false)
                {
                    RestartGameButton.gameObject.SetActive(true);
                }
            }

            var time = system.World.GetExistingSystemManaged<TimeSystem>().GameTime;

            if (system.TryGetSingleton(out NetworkTime netTime))
            {
                time = netTime.Value;
            }

            var waitTime = (float)(time - arenaState.MatchEndTime);
            var elapsedTime = (int)(arenaState.DecisionWaitTime - waitTime);
            TimerCounter.text = $"{math.clamp(elapsedTime, 0, int .MaxValue)}";

            if (isExiting == false && waitTime >= arenaState.DecisionWaitTime)
            {
                isExiting = true;
                DecisionWindow.SetVisible(false);
                
                var matchSystem = EntityManager.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
                var playerEntity = GetData<PlayerController>().Value;
                matchSystem.NotifyExitFromGame(playerEntity);
            }
        }

        public void ShowDecisionWindow()
        {
            DecisionWindow.SetVisible(true);

            ExitFromGameButton.gameObject.SetActive(true);
            ContinueGameButton.gameObject.SetActive(true);
            TimerCounter.gameObject.SetActive(true);
            WaitingOthersText.gameObject.SetActive(false);
        }

        public void OnContinueGamePressed()
        {
            continueGame();
        }

        void continueGame()
        {
            ExitFromGameButton?.gameObject.SetActive(false);
            ContinueGameButton?.gameObject.SetActive(false);
            RestartGameButton?.gameObject.SetActive(false);
            WaitingOthersText?.gameObject.SetActive(true);

            var matchSystem = EntityManager.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
            matchSystem.RequestContinueGame(GetData<PlayerController>().Value);

            DecisionWindow.SetVisible(false);
            LoadingWindow.SetVisible(true);
        }

        public void OnRestartGamePressed()
        {
            continueGame();
        }

        public void OnExitFromGamePressed()
        {
            DecisionWindow?.SetVisible(false);
            ConfirmExitWindow?.SetVisible(true);
        }

        public void OnConfirmExitPressed()
        {
            ExitingWindow.SetVisible(true);
            ConfirmExitWindow.SetVisible(false);

            var matchSystem = EntityManager.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
            matchSystem.NotifyExitFromGame(GetData<PlayerController>().Value);
        }

        public void OnCancelExitPressed()
        {
            ConfirmExitWindow.SetVisible(false);
            ShowDecisionWindow();
        }
    }
}
