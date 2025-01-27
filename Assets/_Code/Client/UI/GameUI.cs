// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections.Generic;
using TzarGames.Common;
using TzarGames.GameFramework;
using UnityEngine;
using UnityEngine.UI;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using TzarGames.GameCore;
using System.Collections;
using Arena.Client;
using Arena;
using UnityEngine.Events;

namespace Arena.Client.UI
{
    public class GameUI : GameUIBase
    {
        [SerializeField] private string uiWindowTag = "UI Window";

        [SerializeField]
        PlayerCharacterUI hud = default;

        [SerializeField]
        GameUIBase mapMenu = default;

        [SerializeField]
        GameUIBase cutsceneWindow = default;

        [SerializeField]
        GameplayMenuUI gameplayMenu = default;

        [SerializeField]
        GameUIBase disconnectedMenu = default;

        [SerializeField]
        GameUIBase matchStateUI = default;

        [SerializeField] private TravelLocationWindowUI locationLocationWindow;

        [SerializeField]
        XpPointManagementUI xpManagement = default;

        [SerializeField]
        AlertUI alert = default;

        [SerializeField]
        Button settingsButton = default;

        [SerializeField]
        Button inventoryButton = default;

        [SerializeField]
        FadeUI fadeBackground = default;

        [SerializeField]
        private DialogueUI dialogueUI = default;

        public DialogueUI DialogueUI => dialogueUI;

        [SerializeField]
        Arena.WorldObserver.WorldObserverUI worldObserver = default;

        [SerializeField] private GameObject xpManagementButton = default;

        [SerializeField] private RubyShopUI rubyShop = default;

        [SerializeField] private ShopUI itemShop = default;
        [SerializeField] private InventoryUI inventory = default;

        [SerializeField] private TzarGames.GameFramework.UI.GameUIBase loading = default;

        List<GameUIBase> allMenus = new List<GameUIBase>();
        List<GameUIBase> gameMenus = new List<GameUIBase>();
        private List<UiBaseState> stateHistory = new List<UiBaseState>();

        [SerializeField] private LocalizedStringAsset stageMessage = default;
        [SerializeField] private LocalizedStringAsset newLevelMessage = default;

        private TzarGames.MultiplayerKit.Client.ClientSystem clientSystem;

        [SerializeField] private UnityEvent onSceleLoaded;

        public PlayerCharacterUI HUD
        {
            get
            {
                return hud;
            }
        }

        protected virtual void Start()
        {
            base.Start();
            
            if (GameState.Instance != null)
            {
                GameState.Instance.OnLoadingStarted += gameState_onLoadingStarted;
                GameState.Instance.OnGameContinued += EndlessGameStateOnOnGameContinued;
                xpManagementButton.SetActive(false);
            }
        }

        public void InitializeUiEntity(Entity entity, EntityManager manager)
        {
            foreach(var menu in gameMenus)
            {
                manager.AddComponentObject(entity, menu);
            }
        }

        private void EndlessGameStateOnOnGameContinued()
        {
            DisableAndFadeInHalf(null);
        }

        class UiBaseState : State
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                UI.stateHistory.RemoveAll((x) => { return x == this; });
                UI.stateHistory.Add(this);

                if (shouldStopPlayerMovement())
                {
                    Debug.Log("stop player movement");
                }
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);

                if (shouldStopPlayerMovement())
                {
                    Debug.Log("Resume player movement");
                }
            }

            protected virtual bool shouldStopPlayerMovement()
            {
                return false;
            }

            public virtual void OnSystemUpdate(UISystem uiSystem)
            {
            }

            public GameUI UI
            {
                get
                {
                    return Owner as GameUI;
                }
            }

            public void ShowOnlyThisMenu(GameUIBase menuToShow)
            {
                UI.showMenu(menuToShow, true);
                foreach (var menu in UI.gameMenus)
                {
                    if (menu != menuToShow)
                    {
                        UI.showMenu(menu, false);
                    }
                }
            }

            public void HideAllMenus()
            {
                foreach (var menu in UI.gameMenus)
                {
                    UI.showMenu(menu, false);
                }
            }
        }

        [DefaultState]
        class Empty : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.allMenus.AddRange(UI.GetComponentsInChildren<GameUIBase>());
                UI.allMenus.Remove(UI);
                UI.gameMenus.AddRange(UI.allMenus);
                UI.gameMenus.RemoveAll(x => x.CompareTag(UI.uiWindowTag) == false);

                foreach (var menu in UI.gameMenus)
                {
                    UI.showMenu(menu, false);
                }
            }
        }

        class Exit : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                
                foreach (var menu in UI.gameMenus)
                {
                    UI.showMenu(menu, false);
                }
                
                UI.ReturnToMainArea();
            }
        }

        class Gameplay : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                if (GameState.Instance != null && GameState.Instance.Paused)
                {
                    GameState.Instance.Paused = false;
                }

                UI.showMenu(UI.hud, true);
                if (GameState.Instance != null)
                {
                    var inGame = GameState.Instance.IsInGameState();

                    UI.settingsButton.gameObject.SetActive(inGame);
                    UI.inventoryButton.gameObject.SetActive(inGame);
                }
                else
                {
                    UI.settingsButton.gameObject.SetActive(true);
                    UI.inventoryButton.gameObject.SetActive(true);
                }
            }
            
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.hud, false);
                UI.showMenu(UI.matchStateUI, false);
            }

            public override void OnSystemUpdate(UISystem uiSystem)
            {
                base.OnSystemUpdate(uiSystem);
                
                UI.hud.OnSystemUpdate(uiSystem);
                
                if(uiSystem.TryGetSingleton(out ArenaMatchStateData matchData))
                {
                    if (matchData.State == ArenaMatchState.WaitingForNextStep)
                    {
                        if(UI.matchStateUI.IsVisible == false)
                        {
                            UI.showMenu(UI.matchStateUI, true);
                        }    
                    }
                }
                else
                {
                    if(UI.matchStateUI.IsVisible)
                    {
                        UI.showMenu(UI.matchStateUI, false);
                    }    
                }

                if(UI.matchStateUI.IsVisible)
                {
                    UI.matchStateUI.OnSystemUpdate(uiSystem);
                }
            }
        }

        class GameplayMenu : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.showMenu(UI.gameplayMenu, true);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.gameplayMenu, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }

            public override void OnSystemUpdate(UISystem uiSystem)
            {
                base.OnSystemUpdate(uiSystem);
                
                if (UI.gameplayMenu.IsVisible)
                {
                    UI.gameplayMenu.OnSystemUpdate(uiSystem);
                }
            }
        }

        //class MatchStateMenu : UiBaseState
        //{
        //    public override void OnStateBegin(State prevState)
        //    {
        //        base.OnStateBegin(prevState);
        //        UI.showMenu(UI.matchStateUI, true);
        //        UI.showFadingBackground(false);
        //    }
        //    public override void OnStateEnd(State nextState)
        //    {
        //        base.OnStateEnd(nextState);
        //        UI.showMenu(UI.matchStateUI, false);
        //        UI.showFadingBackground(false);
        //    }

        //    public override void OnSystemUpdate(UISystem uiSystem)
        //    {
        //        base.OnSystemUpdate(uiSystem);
        //        UI.matchStateUI.OnSystemUpdate(uiSystem);
        //    }
        //}

        class DisconnectedMenu : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.showMenu(UI.disconnectedMenu, true);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.disconnectedMenu, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }
        
        public void ShowQuestList()
        {
            GotoState<LocationWindowState>();
        }
        
        class LocationWindowState : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.showMenu(UI.locationLocationWindow, true);
                UI.showFadingBackground(true);
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.locationLocationWindow, false);
                UI.showFadingBackground(false);
            }
        }

        class Map : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                //				if(EndlessGameState.Instance != null)
                //				{
                //					EndlessGameState.Instance.Paused = true;
                //				}
                UI.showMenu(UI.mapMenu, true);
                UI.showFadingBackground(true);

                var map = FindObjectOfType<Arena.Map>();
                if (map != null)
                {
                    map.SetExtendedMode();
                }
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.mapMenu, false);
                UI.showFadingBackground(false);

                var map = FindObjectOfType<Arena.Map>();
                if (map != null)
                {
                    map.SetMinimapMode();
                }
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        class Dialogue : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                ShowOnlyThisMenu(UI.dialogueUI);
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.dialogueUI, false);
            }
        }

        [ContextMenu("test dialogue")]
        public void TestDialogue()
        {
            ShowDialogueWindow(true);
            dialogueUI.ShowDialogue(default, default, "Здрасти", new[] { new DialogueAnswerData { Text = "Мордасти", CommandAddress = default } });
        }

        class WorldObserver : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.showMenu(UI.worldObserver, true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.worldObserver, false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        class Cutscene : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.showMenu(UI.cutsceneWindow, true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.cutsceneWindow, false);
            }
        }

        class XpManagement : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);

                UI.showMenu(UI.xpManagement, true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.xpManagement, false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        [Uninterrupted(typeof(Gameplay), typeof(RubyShop))]
        class MatchEnd : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                HideAllMenus();
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.matchStateUI, false);
                UI.showFadingBackground(false);
            }

            public override void OnSystemUpdate(UISystem system)
            {
                base.OnSystemUpdate(system);
                
                if(system.HasSingleton<ArenaMatchStateData>())
                {
                    var matchState = system.GetSingleton<ArenaMatchStateData>();

                    if(matchState.IsMatchComplete)
                    {
                        if(UI.matchStateUI.IsVisible == false)
                        {
                            UI.showMenu(UI.matchStateUI, true);
                        }
                    }
                    else
                    {
                        if(UI.matchStateUI.IsVisible)
                        {
                            UI.showMenu(UI.matchStateUI, false);
                        }
                    }
                }
                
                if(UI.matchStateUI.IsVisible)
                {
                    UI.matchStateUI.OnSystemUpdate(system);
                }
            }
        }

        class RubyShop : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                ShowOnlyThisMenu(UI.rubyShop);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.rubyShop, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }
        
        class ItemShop : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                ShowOnlyThisMenu(UI.itemShop);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.itemShop, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        class Inventory : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                ShowOnlyThisMenu(UI.inventory);
                UI.inventory.RefreshItems();
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.inventory, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        class Loading : UiBaseState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                ShowOnlyThisMenu(UI.loading);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(UI.loading, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        class ForeignMenu : UiBaseState
        {
            private GameUIBase menu;
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                if (Parameters != null && Parameters.Length > 0)
                {
                    menu = Parameters[0] as GameUIBase;
                }

                ShowOnlyThisMenu(menu);
                UI.showFadingBackground(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.showMenu(menu, false);
                UI.showFadingBackground(false);
            }

            protected override bool shouldStopPlayerMovement()
            {
                return true;
            }
        }

        public void ShowAlert(string message)
        {
            alert.Show(message);
        }

        public void ShowRubyShop()
        {
            GotoState<RubyShop>();
        }

        public void ShowItemShop()
        {
            GotoState<ItemShop>();
        }

        public void ShowInventory()
        {
            GotoState<Inventory>();
        }

        public void UpdateInventory()
        {
            inventory.UpdateUI();
        }

        public void ShowMap()
        {
            GotoState<Map>();
        }

        public void ShowWorldObserver()
        {
            GotoState<WorldObserver>();
        }

        public void ShowPreviousStateMenu()
        {
            if (stateHistory.Count > 0)
            {
                stateHistory.RemoveAt(stateHistory.Count - 1);
                if (stateHistory.Count > 0)
                {
                    GotoState(stateHistory[stateHistory.Count - 1].GetType());
                }
            }
        }

        public void ShowGameHUD()
        {
            GotoState<Gameplay>();
        }

        public void ShowGameplayMenuWithMode(bool show, bool exitMode)
        {
            if (show)
            {
                gameplayMenu.ExitMode = exitMode;
                GotoState<GameplayMenu>();
            }
            else
            {
                GotoState<Gameplay>();
            }
        }

        public void ShowGameplayMenu(bool show)
        {
            ShowGameplayMenuWithMode(show, false);
        }

        public void ShowLoading()
        {
            GotoState<Loading>();
        }

        public void ShowXpManagement()
        {
            GotoState<XpManagement>();
        }

        public void DisableAndFadeIn(Action onCompleteCallback = null)
        {
            GotoState<Empty>();
            showFadingBackground(true, true, onCompleteCallback);
        }

        public void DisableAndFadeInHalf(Action onCompleteCallback = null)
        {
            GotoState<Empty>();
            showFadingBackground(true, false, onCompleteCallback);
        }

        public void FadeIn(System.Action onCompleteCallback = null)
        {
            showFadingBackground(true, true, onCompleteCallback);
        }

        public void FadeOut(System.Action onCompleteCallback = null)
        {
            showFadingBackground(false, true, onCompleteCallback);
        }

#if UNITY_EDITOR || TZAR_TESTING
        [ConsoleCommand]
        public void DisableUI()
        {
            GotoState<Empty>();
            Cursor.visible = false;
        }

        [ConsoleCommand]
        public void EnableUI()
        {
            GotoState<Gameplay>();
            Cursor.visible = true;
        }
#endif

        public void Disable()
        {
            GotoState<Empty>();
        }

        void showFadingBackground(bool show, bool full = false, System.Action onCompleteCallback = null)
        {
            if (show)
            {
                if (fadeBackground.enabled == false)
                {
                    fadeBackground.enabled = true;
                }

                if (full)
                {
                    fadeBackground.FadeInFull(onCompleteCallback);
                }
                else
                {
                    fadeBackground.FadeInHalf(onCompleteCallback);
                }
            }
            else
            {
                fadeBackground.FadeOut(onCompleteCallback);
            }
        }

        public void ShowForeignMenu(GameUIBase menu)
        {
            GotoState<ForeignMenu>(menu);
        }

        void showMenu(GameUIBase menu, bool show)
        {
#if UNITY_EDITOR
            if (menu.CompareTag(uiWindowTag) == false)
            {
                Debug.LogError("Menu window " + menu + " is not tagged as " + uiWindowTag);
            }
#endif
            menu.SetVisible(show);
        }

        public void ExitToMainMenu()
        {
            GotoState<Empty>();
            showFadingBackground(true, true);
            GameState.Instance.ExitToMainMenu();
        }

        public void ReturnToMainArea()
        {
            GotoState<Empty>();
            StartCoroutine(gotoMainArea());
        }

        IEnumerator gotoMainArea()
        {
            yield return new WaitForSeconds(.5f);
            
            var clientSystem = EntityManager.World.GetExistingSystemManaged<ClientArenaMatchSystem>();
            clientSystem.NotifyExitFromGame(false, GetData<PlayerController>().Value);
        }

        public void ShowCutsceneWindow(bool show)
        {
            if(show)
            {
                GotoState<Cutscene>();
            }
            else
            {
                if(CurrentState is Cutscene)
                {
                    ShowPreviousStateMenu();
                }
            }
        }

        public void ShowDialogueWindow(bool show)
        {
            if (show)
            {
                GotoState<Dialogue>();
            }
            else
            {
                ShowGameHUD();
            }
        }

        public override void OnSystemUpdate(UISystem system)
        {
            base.OnSystemUpdate(system);

            if (CurrentState is Exit)
            {
                return;
            }

            (CurrentState as UiBaseState).OnSystemUpdate(system);
            
            if (clientSystem != null)
            {
                if (clientSystem.IsConnected)
                {
                    if (CurrentState is DisconnectedMenu)
                    {
                        GotoState<Gameplay>();
                    }
                }
                else
                {
                    if (HasData<Player>(OwnerEntity))
                    {
                        if (CurrentState is Gameplay == false)
                        {
                            GotoState<Gameplay>();
                        }
                    }
                    else
                    {
                        if (system.TryGetSingleton(out ArenaMatchStateData stateData))
                        {
                            if (stateData.State == ArenaMatchState.Finished
                                || stateData.State == ArenaMatchState.WaitingForNextStep)
                            {
                                GotoState<Exit>();
                            }
                            else
                            {
                                if (CurrentState is DisconnectedMenu == false)
                                {
                                    GotoState<DisconnectedMenu>();
                                }    
                            }
                        }
                        else
                        {
                            if (CurrentState is DisconnectedMenu == false)
                            {
                                GotoState<DisconnectedMenu>();
                            }
                        }
                    }
                    return;
                }
            }

            if (EntityManager.Exists(OwnerEntity) == false)
            {
                return;
            }

            if (HasData<LivingState>() == false)
            {
                return;
            }

            var state = GetData<LivingState>();

            if (state.IsAlive == false)
            {
                if (CurrentState is MatchEnd == false)
                {
                    GotoState<MatchEnd>();
                }
            }
            else
            {
                if (CurrentState is MatchEnd)
                {
                    GotoState<Gameplay>();
                }
            }
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);

            clientSystem = manager.World.GetExistingSystemManaged<TzarGames.MultiplayerKit.Client.ClientSystem>();

            foreach(var menu in allMenus)
            {
	            if (menu != this)
	            {
		            menu.Setup(ownerEntity, uiEntity, manager);    
	            }
            }
			
            GotoState<Gameplay>();

            StartCoroutine(startFade(manager));

            //hud.Character.OnCharacterDead += onPlayerCharacterDead;
            //hud.Character.OnCharacterAlive += onPlayerCharacterAlive;
            //hud.Character.OnCharacterLevelUp += CharacterOnOnCharacterLevelUp;
        }

        IEnumerator startFade(EntityManager manager)
        {
            var renderingSystem = manager.World.GetExistingSystemManaged<TzarGames.Rendering.RenderingSystem>();

            while (renderingSystem.LoadingMaterialCount > 0 || renderingSystem.LoadingMeshCount > 0)
            {
                yield return null;
            }
            onSceleLoaded.Invoke();
        }

		//private void CharacterOnOnCharacterLevelUp(Character character)
		//{
		//	alert.Show(newLevelMessage);
		//}

        public void ShowLevelUpAlert()
        {
            alert.Show(newLevelMessage);
        }

		void OnDestroy()
		{
            if (GameState.Instance != null)
            {
                GameState.Instance.OnLoadingStarted -= gameState_onLoadingStarted;
                GameState.Instance.OnGameContinued -= EndlessGameStateOnOnGameContinued;    
            }
        }

		void gameState_onLoadingStarted()
		{
			ShowLoading();
		}
    }

}
