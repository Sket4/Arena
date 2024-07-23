// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using Arena.Client;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.ScriptViz;
using TzarGames.MatchFramework.Client;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Arena.Client.UI.MainMenu
{
    public class MainUI : MonoBehaviour
    {
        public static readonly string EnableCharactersMessage = "characters_enable";
        public static readonly string DisableCharactersMessage = "characters_disable";
        string uiWindowTag = "UI Window";

        [SerializeField]
        UIBase mainMenu = default;

        [SerializeField]
        UIBase connectingWindow = default;

        [SerializeField]
        LoadingUI loading = default;

        [SerializeField]
        CreateCharacterUI createCharacterMenu = default;

        [SerializeField]
        SettingsUI settingsUI = default;

        [SerializeField] private CharacterEditorUI characterEditorUI;

        [SerializeField] private SimpleLocalGameLauncher gameLauncher;

        List<UIBase> windows = new List<UIBase>();

        UIBase currentWindow;

        public World SceneWorld => gameLauncher.GameLoop?.World;
        
        public UnityEvent OnSceneLoaded;

        void Start()
        {
            StartCoroutine(initSceneWorld());
            
            var windowObjects = GameObject.FindGameObjectsWithTag(uiWindowTag);

            foreach (var windowObject in windowObjects)
            {
                var window = windowObject.GetComponent<UIBase>();
                if (window == null)
                {
                    Debug.LogFormat("Invalid window {0} with tag {1}", windowObject.name, tag);
                    continue;
                }
                windows.Add(window);
            }

            characterEditorUI.OnCharacterSelectedLoaded += () =>
            {
                ShowWindow(mainMenu);
            };

            if (GameState.Instance == null)
            {
                ShowWindow(mainMenu);
                return;
            }

            GameState.Instance.OnMainMenuLoaded += GameState_OnMainMenuLoaded;

            if(needToCreateCharacter())
            {
                createCharacterMenu.OnGoToNextScene += CreateCharacterMenu_OnGoToNextScene;
                createCharacterMenu.SetCancelState(false);
                createCharacterMenu.SetNextMenu(mainMenu);
                ShowWindow(createCharacterMenu);
            }
            else
            {
                createCharacterMenu.SetCancelState(true);
                ShowWindow(mainMenu);
            }
        }
        
        private void OnDestroy()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.OnMainMenuLoaded -= GameState_OnMainMenuLoaded;
            }
        }

        IEnumerator initSceneWorld()
        {
            yield return WaitForSceneGameLoop(null);
            Utils.AddSharedSystems(gameLauncher.GameLoop, true, "Menu");

            gameLauncher.GameLoop.World.GetExistingSystemManaged<CharacterRotationSystem>().Enabled = false;
            
            gameLauncher.GameLoop.AddGameSystem<CharacterItemAppearanceSystem>();
            gameLauncher.GameLoop.AddGameSystem<AnimationSystem>();
            gameLauncher.GameLoop.AddGameSystem<CharacterModelSmoothMovementSystem>();
            gameLauncher.GameLoop.AddGameSystem<MaterialRenderingSystem>(gameLauncher.GameLoop.World.GetExistingSystemManaged<PresentationSystemGroup>());

            Debug.Log("start waiting for mesh and material loading");
            var renderingSystem = gameLauncher.GameLoop.World.GetExistingSystemManaged<RenderingSystem>();

            while (renderingSystem.LoadingMaterialCount > 0 || renderingSystem.LoadingMeshCount > 0)
            {
                yield return null;
            }
            
            Debug.Log("finished waiting for mesh and material loading");

            yield return null;
            
            OnSceneLoaded.Invoke();
        }

        void GameState_OnMainMenuLoaded()
        {
            if (createCharacterMenu.IsVisible == false && mainMenu.IsVisible == false)
            {
                ShowWindow(mainMenu);
            }
        }

        public IEnumerator WaitForSceneGameLoop(System.Action<GameLoopBase> gameLoopCallback)
        {
            while (gameLauncher.GameLoop == null || gameLauncher.IsLoadingScenes())
            {
                yield return null;
            }
            gameLauncher.GameLoop.Update();
            gameLoopCallback?.Invoke(gameLauncher.GameLoop);
        }
        
        public void ShowWindow(UIBase window)
        {
            if(windows.Contains(window) == false)
            {
                Debug.LogErrorFormat("Window {0} is not added to the windows list", window.name);
                return;
            }

            window.SetVisible(true);

            foreach(var w in windows)
            {
                if(w == window)
                {
                    continue;
                }
                w.SetVisible(false);
            }
        }

        bool needToCreateCharacter()
        {
            return GameState.Instance.PlayerData.Characters.Count < 1 || GameState.Instance.SelectedCharacter == null;
        }

        private void CreateCharacterMenu_OnGoToNextScene()
        {
            createCharacterMenu.OnGoToNextScene -= CreateCharacterMenu_OnGoToNextScene;
            ShowWindow(loading);
            
            GameState.Instance.StartGame();
        }

        public void CloseSettings()
        {
            ShowWindow(mainMenu);
        }
    }
}
