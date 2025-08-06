// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System.Collections;
using Arena.Client;
using Arena.Server;
using TzarGames.Common.UI;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class MainMenuUI : UIBase
    {
        [SerializeField]
        TextUI stageLabel = default;

        [SerializeField]
        TextUI characterNameLabel = default;


	    [SerializeField] private UIBase autenticatingStatus = default;
	    [SerializeField] private UIBase socialMenu = default;

        [SerializeField] private Button continueButton = default;


        [SerializeField]
        private GameObject rateButton = default;

        private Entity currentCharacterEntity = Entity.Null;

	    void setSocialAuthenticating(bool on)
	    {
		    autenticatingStatus.SetVisible(on);
		    socialMenu.SetVisible(!on);
	    }

        protected override void OnVisible()
        {
            base.OnVisible();
            
            if (GameState.Instance == null)
            {
                return;
            }
            
            var mainUI = FindObjectOfType<MainUI>();
            
            StartCoroutine(mainUI.WaitForSceneGameLoop((gameLoop) =>
            {
                var utilSystem = gameLoop.World.GetExistingSystemManaged<UtilitySystem>();

                var selectedCharacter = GameState.Instance.SelectedCharacter;

                if (currentCharacterEntity != Entity.Null)
                {
                    utilSystem.EntityManager.DestroyEntity(currentCharacterEntity);
                    currentCharacterEntity = Entity.Null;
                }
                
                if (utilSystem.HasSingleton<PlayerController>())
                {
                    var character = utilSystem.GetSingletonEntity<PlayerController>();
                    utilSystem.EntityManager.DestroyEntity(character);
                }

                if (selectedCharacter != null)
                {
                    characterNameLabel.text = selectedCharacter.Name;
                    currentCharacterEntity = MainUI.CreateCharacter(utilSystem, selectedCharacter);  
                }
            }));
        }

        protected override void OnHidden()
        {
            base.OnHidden();

            var mainUI = FindObjectOfType<MainUI>();
            
            StartCoroutine(mainUI.WaitForSceneGameLoop(gameLoop =>
            {
                var em = gameLoop.World.EntityManager;
                
                if (em.Exists(currentCharacterEntity))
                {
                    em.DestroyEntity(currentCharacterEntity);
                }
                
                var utilSystem = gameLoop.World.GetExistingSystemManaged<UtilitySystem>();
                if (utilSystem.HasSingleton<PlayerController>())
                {
                    var character = utilSystem.GetSingletonEntity<PlayerController>();
                    utilSystem.EntityManager.DestroyEntity(character);
                }
                
                currentCharacterEntity = Entity.Null;
            }));
        }

        public void ShowLeaderboard()
	    {
            Debug.LogError("Not implemented");
//		    if (SocialSystem.Instance.IsAuthenticated)
//		    {
//			    SocialSystem.Instance.ShowDefaultLeaderboardUI();    
//		    }
//		    else
//		    {
//			    setSocialAuthenticating(true);
//			    SocialSystem.Instance.Authenticate((success, message) =>
//			    {
//				    setSocialAuthenticating(false);
////				    Debug.Log("Social authentication: " + success + " msg: " + message);
//				    if (success)
//				    {
//					    SocialSystem.Instance.ShowDefaultLeaderboardUI();
//				    }
//			    });
//		    }
	    }

        public void ContinueGame()
        {
            StartCoroutine(continueGameRoutine());
        }
        
        IEnumerator continueGameRoutine()
        {
            yield return new WaitForSeconds(1);
            GameState.Instance.StartGame();
        }

	    protected override void Start()
	    {
		    base.Start();

            var game = GameState.Instance;

            bool canRate = true;
#if UNITY_IOS
            var version = UnityEngine.iOS.Device.systemVersion;
            var splitVersion = version.Split('.');

            if(splitVersion != null && splitVersion.Length > 1)
            {
                int mainVersionInt;
                int subVersionInt;
                if(int.TryParse(splitVersion[0], out mainVersionInt)
                    && int.TryParse(splitVersion[1], out subVersionInt))
                {
                    if(mainVersionInt < 10 || (mainVersionInt == 10 && subVersionInt < 3))
                    {
                        canRate = false;
                    }
                }
            }
#endif
            rateButton.SetActive(canRate);
	    }
    }
}
