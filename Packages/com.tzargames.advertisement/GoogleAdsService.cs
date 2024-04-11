using System;
using UnityEngine;

namespace TzarGames.Ads
{
    public class GoogleAdsService : AdService
    {
        [SerializeField] private string androidAppId = default;
        [SerializeField] private string iosAppId = default;
        
        [SerializeField] private string androidAdUnitId = default;
        [SerializeField] private string iosAdUnitId = default;

        [SerializeField]
        bool debug = false;
        
        //private RewardBasedVideoAd videoAd;

        private bool isShowing = false;
        private Action<ShowResult> lastCallback = null;

        bool canReward = false;
        
        public override bool IsReady(Ad ad)
        {   
            if (isShowing)
            {
                return false;
            }
            
#if UNITY_EDITOR
            return true;
#endif 

            return false;
            //return videoAd.IsLoaded();
        }

        public override void Show(Ad ad, Action<ShowResult> callback)
        {
            if (isShowing)
            {
                callback(ShowResult.Failed);
                return;
            }
            
            #if UNITY_EDITOR
            callback(ShowResult.Finished);
            return;
            #endif 
            
            //if (videoAd.IsLoaded())
            //{
            //    lastCallback = callback;
            //    isShowing = true;
                
            //    try
            //    {
            //        videoAd.Show();
            //        NotifyAdStarted();
            //    }
            //    catch (Exception e)
            //    {
            //        isShowing = false;
            //        lastCallback = null;
            //        Debug.LogException(e);
            //        NotifyAdSkippedOrFinished();
            //        callback(ShowResult.Failed);
            //    }
            //}
        }

        public override void Initialize()
        {
            if(debug)
            {
                Debug.Log("Google Mobile Ads initialization started");
            }
            
            string appId;
#if UNITY_ANDROID
            appId = androidAppId;
#elif UNITY_IOS
            appId = iosAppId;
#else
            appId = null;
#endif
            
            //MobileAds.Initialize(appId);

            //videoAd = RewardBasedVideoAd.Instance;
            //isShowing = false;

            //// Called when an ad request has successfully loaded.
            //videoAd.OnAdLoaded += HandleRewardBasedVideoLoaded;
            //// Called when an ad request failed to load.
            //videoAd.OnAdFailedToLoad += HandleRewardBasedVideoFailedToLoad;
            //// Called when an ad is shown.
            //videoAd.OnAdOpening += HandleRewardBasedVideoOpened;
            //// Called when the ad starts to play.
            //videoAd.OnAdStarted += HandleRewardBasedVideoStarted;
            //// Called when the user should be rewarded for watching a video.
            //videoAd.OnAdRewarded += HandleRewardBasedVideoRewarded;
            //// Called when the ad is closed.
            //videoAd.OnAdClosed += HandleRewardBasedVideoClosed;
            //// Called when the ad click caused the user to leave the application.
            //videoAd.OnAdLeavingApplication += HandleRewardBasedVideoLeftApplication;
            
            requestAd();
        }

        public override void RequestAd(Ad ad)
        {
            requestAd();
        }

        void requestAd()
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads request video ad");
            }
            //AdRequest request = new AdRequest.Builder().Build();
            string adUnitId = null;
#if UNITY_ANDROID
#if UNITY_EDITOR
            adUnitId = "ca-app-pub-3940256099942544/5224354917";
#else
            adUnitId = androidAdUnitId;
#endif
#elif UNITY_IOS
#if UNITY_EDITOR
            adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
            adUnitId = iosAdUnitId;
#endif
#else
            adUnitId = "unexpected_platform";
#endif

            canReward = false;
            //videoAd.LoadAd(request, adUnitId);
        }

        private void HandleRewardBasedVideoOpened(object sender, EventArgs e)
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads HandleRewardBasedVideoOpened");
            }
        }

        private void HandleRewardBasedVideoLeftApplication(object sender, EventArgs e)
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads HandleRewardBasedVideoLeftApplication");
            }
        }

        private void HandleRewardBasedVideoClosed(object sender, EventArgs e)
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads HandleRewardBasedVideoClosed");
            }
            if(canReward)
            {
                if(canReward)
                {
                    canReward = false;
                    finish(ShowResult.Finished, true);
                }
            }
            else
            {
                finish(ShowResult.Skipped, true);
            }
        }

        //private void HandleRewardBasedVideoRewarded(object sender, Reward e)
        //{
        //    if (debug)
        //    {
        //        Debug.Log("Google Mobile Ads HandleRewardBasedVideoClosed");
        //    }
        //    canReward = true;
        //}

        private void HandleRewardBasedVideoStarted(object sender, EventArgs e)
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads HandleRewardBasedVideoStarted");
            }
        }

        //private void HandleRewardBasedVideoFailedToLoad(object sender, AdFailedToLoadEventArgs e)
        //{
        //    if (debug)
        //    {
        //        Debug.Log("Google Mobile Ads HandleRewardBasedVideoFailedToLoad");
        //    }
        //    finish(ShowResult.Failed, false);
        //}

        private void HandleRewardBasedVideoLoaded(object sender, EventArgs e)
        {
            if (debug)
            {
                Debug.Log("Google Mobile Ads HandleRewardBasedVideoLoaded");
            }
        }

        void finish(ShowResult result, bool requestAgain)
        {
            isShowing = false;
            NotifyAdSkippedOrFinished();

            if (requestAgain)
            {
                requestAd();    
            }
            
            if (lastCallback != null)
            {
                var tmp = lastCallback;
                lastCallback = null;
                tmp(result);
            }
        }
    }
}
