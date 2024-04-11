// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using TzarGames.Common;
using TzarGames.Ads;
using TzarGames.Common.UI;
using TzarGames.GameFramework.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class DeathUI : GameUIBase
	{
		[SerializeField] private UIBase mainWindow = default;
		[SerializeField] private UIBase waitingWindow = default;
		[SerializeField] private UIBase loseProgressDialog = default;
        [SerializeField] private UIBase hardcoreDeadWindow = default;
        [SerializeField] private UIBase restartWindow = default;
        [SerializeField] private UIBase noAdsWindow = default;


        [SerializeField] private GameUI gameUI = default;
		
		[SerializeField]
		ShowAds ads = default;
        
		[SerializeField] private CustomAnalyticsEventTracker _eventTracker = default;

		//[SerializeField] private GameObject respawnByRubyButton = default;

        public void Restart()
        {
            restartWindow.SetVisible(false);
        }

		public void NotifyVisible()
		{
            loseProgressDialog.SetVisible(false);
			waitingWindow.SetVisible(false);

            var gameState = GameState.Instance;

            if(gameState != null)
            {
                //if (gameState.SelectedCharacter.Dead)
                //{
                //    hardcoreDeadWindow.SetVisible(true);
                //    mainWindow.SetVisible(false);
                //    return;
                //}
            }
			
			mainWindow.SetVisible(true);
            noAdsWindow.SetVisible(false);
			

			if (ads.IsReady() == false)
			{
				ads.RequestAd();
			}
		}

		public void OnReturnToMainAreaClicked()
		{
			mainWindow.SetVisible(false);
			loseProgressDialog.SetVisible(true);
		}

		public void CancelReturnToMainArea()
		{
			mainWindow.SetVisible(true);
			loseProgressDialog.SetVisible(false);
		}

        public void CancelNoAdsWindow()
        {
            mainWindow.SetVisible(true);
            noAdsWindow.SetVisible(false);
        }

        public void ConfirmReturnToMainAreaClicked()
		{
			loseProgressDialog.SetVisible(false);
            mainWindow.SetVisible(false);
            hardcoreDeadWindow.SetVisible(false);
            throw new System.NotImplementedException();
            // GameState.Instance.GotoArea(new AreaRequest 
            // { 
            //     Area = Arena.GameLocationType.SafeZone, Multiplayer = false 
            // });
        }

		public void OnRestartClicked()
		{
			mainWindow.SetVisible(false);
			loseProgressDialog.SetVisible(true);
		}

		public void ConfirmRestartClicked()
		{
			loseProgressDialog.SetVisible(false);
			mainWindow.SetVisible(false);
			hardcoreDeadWindow.SetVisible(false);

			waitingWindow.SetVisible(true);

			var matchSystem = EntityManager.World.GetExistingSystemManaged<Arena.Client.ClientArenaMatchSystem>();

			throw new NotImplementedException();
			//matchSystem.RequestRestartAndRevive(GetData<PlayerController>().Value);
		}

		public void OnWatchAdvertPressed()
		{
			if (ads.IsReady())
			{
				ads.ShowAd();	
			}
			else
			{
                mainWindow.SetVisible(false);
                noAdsWindow.SetVisible(true);
			}
		}

		public void OnRespawnByRubyPressed()
		{
            Debug.LogError("Not implemented");

            //var gameManager = (EndlessGameManager.Instance as EndlessStoryGameManager);
            //if (gameManager.CanRespawnLocalPlayerByRuby())
            //{
            //	gameManager.RespawnLocalPlayerByRuby();
            //}
            //else
            //{
            //	gameUI.ShowRubyShop();
            //}
        }

		public void OnAdvertWatched()
		{
            Debug.LogError("Not implemented");
            //var gameManager = (EndlessGameManager.Instance as EndlessStoryGameManager);
            //if (gameManager.CanLocalPlayerRespawnedByAdvert())
            //{
            //	gameManager.RespawnLocalPlayerByAdvert();	
            //}
            //else
            //{
            //	Debug.LogError("Undexpected");
            //}
        }

        //private void Update()
        //{
        //    if (mainWindow.gameObject.activeInHierarchy && mainWindow.IsVisible)
        //    {
        //        var gameManager = (EndlessGameManager.Instance as EndlessStoryGameManager);
        //        respawnByAdsButton.SetActive(gameManager.CanLocalPlayerRespawnedByAdvert());
        //    }
            
        //    if(noAdsWindow.gameObject.activeInHierarchy && noAdsWindow.IsVisible)
        //    {
        //        if(ads.IsReady())
        //        {
        //            noAdsWindow.SetVisible(false);
        //            ads.ShowAd();
        //        }
        //    }
        //}
    }
}
