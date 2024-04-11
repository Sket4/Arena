// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using TzarGames.Ads;
using TzarGames.Common.UI;
using TzarGames.GameFramework.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class BuyGameUI : GameUIBase
    {
        [SerializeField] private UIBase waitingWindow = default;
        [SerializeField] private UIBase mainWindow = default;
        [SerializeField] private UIBase noAdsWindow = default;
        
        [SerializeField]
        private ShowAds ads = default;

        public event Action OnContinueAllowed;

        protected override void Start()
        {
            base.Start();
            ads.OnAdFinished.AddListener(adsFinished);
            ads.OnAdSkipped.AddListener(OnAddCancelled);
        }

        private void OnAddCancelled()
        {
            mainWindow.SetVisible(true);
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            
            mainWindow.SetVisible(true);
            waitingWindow.SetVisible(false);
            noAdsWindow.SetVisible(false);
            
            if (ads.IsReady() == false)
            {
                ads.RequestAd();
            }
        }

        public void OnBackFromNoAdsWindowPressed()
        {
            mainWindow.SetVisible(true);
            noAdsWindow.SetVisible(false);
        }

        void adsFinished()
        {
            GameState.Instance.NotifyGameAdsWatched();
            continueGame();
        }

        void continueGame()
        {
            GameState.Instance.TryContinueGame();

            if (GameState.Instance.IsInGameState())
            {
                mainWindow.SetVisible(false);
                waitingWindow.SetVisible(false);
                noAdsWindow.SetVisible(false);
            }
            else
            {
                mainWindow.SetVisible(true);
            }
            
            if (OnContinueAllowed != null)
            {
                OnContinueAllowed();
            }
        }

        public void CancelBuy()
        {
            GameState.Instance.CancelGameContinue();
        }
        
        public void OnWatchAdsPressed()
        {
            mainWindow.SetVisible(false);
            waitingWindow.SetVisible(false);

            if(ads.IsReady())
            {
                ads.ShowAd();    
            }
            else
            {
                noAdsWindow.SetVisible(true);
            }
        }

        private void Update()
        {
            if(noAdsWindow.IsVisible)
            {
                if(ads.IsReady())
                {
                    ads.ShowAd();
                }
            }
        }
    }
}
