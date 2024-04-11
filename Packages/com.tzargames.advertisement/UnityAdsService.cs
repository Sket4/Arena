#if UNITY_ADS
using UnityEngine.Advertisements;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TzarGames.Ads
{
    class AdsShowListener
#if UNITY_ADS
        : IUnityAdsShowListener
#endif
    {
        public Action<ShowResult> FinishCallback { get; private set; }

        public AdsShowListener(Action<ShowResult> finishCallback)
        {
            FinishCallback = finishCallback;
        }

#if UNITY_ADS
        public void OnUnityAdsShowClick(string placementId)
        {
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            switch (showCompletionState)
            {
                case UnityAdsShowCompletionState.SKIPPED:
                    log(showCompletionState);
                    FinishCallback(ShowResult.Skipped);
                    break;
                case UnityAdsShowCompletionState.COMPLETED:
                    FinishCallback(ShowResult.Finished);
                    break;
                case UnityAdsShowCompletionState.UNKNOWN:
                    log(showCompletionState);
                    FinishCallback(ShowResult.Failed);
                    break;
                default:
                    log(showCompletionState);
                    FinishCallback(ShowResult.Failed);
                    break;
            }
        }

        void log(UnityAdsShowCompletionState state)
        {
            Debug.Log(state.ToString());
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.Log($"Ads show failed, error: {error}, message: {message}");
            FinishCallback(ShowResult.Failed);
        }

        public void OnUnityAdsShowStart(string placementId)
        {
        }
#endif
    }

    public class UnityAdsService : AdService
#if UNITY_ADS
        , IUntyAdsInitializanmoovetionListener, IUnityAdsLoadListener
#endif
    {
        [SerializeField]
        string androidGameId;

        [SerializeField]
        string iosGameId;

        [SerializeField]
        bool testMode;


        Dictionary<string, bool> loadedAds = new Dictionary<string, bool>();

        public override bool IsReady(Ad ad)
        {
#if UNITY_ADS
            if(loadedAds.TryGetValue(ad.AdId, out bool isReady))
            {
                return isReady;
            }
            return false;
#else
            return false;
#endif
        }

        public override void Show(Ad id, Action<ShowResult> callback)
        {
#if UNITY_ADS
            if (Advertisement.isInitialized == false)
            {
                Debug.LogError("Advertisement is not yet initialized");
                callback(ShowResult.Failed);
                return;
            }

            Debug.Log("Showing video ad...");

            var listener = new AdsShowListener((result) =>
            {
                Debug.Log("Ads complete callback " + result);
                RequestAd(id);
                NotifyAdSkippedOrFinished();
                callback(result);
            });

            NotifyAdStarted();
            Advertisement.Show(id.AdId, listener);
#endif
        }

        public override void Initialize()
        {
            string gameId;
#if UNITY_ANDROID
            gameId = androidGameId;
#elif UNITY_IOS
            gameId = iosGameId;
#else
            throw new System.NotImplementedException();
#endif

#if UNITY_ADS
            Advertisement.Initialize(gameId, testMode, this);
#endif
        }

        public override void RequestAd(Ad ad)
        {
            requestAd(ad.AdId);
        }

        void requestAd(string ad)
        {
            Debug.Log($"Requesting ad {ad}");
#if UNITY_ADS
            Advertisement.Load(ad, this);
#endif
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            Debug.Log("Ad loaded:" + placementId);
            if(loadedAds.ContainsKey(placementId))
            {
                loadedAds[placementId] = true;
            }
            else
            {
                loadedAds.Add(placementId, true);
            }
        }

#if UNITY_ADS
        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"Ad {placementId} failed to load, error: {error}, message: {message}");
            if (loadedAds.ContainsKey(placementId))
            {
                loadedAds[placementId] = false;
            }
            else
            {
                loadedAds.Add(placementId, false);
            }
            reloadAd(placementId);
        }

        bool isReloading = false;

        async void reloadAd(string placementId)
        {
            if(isReloading)
            {
                return;
            }
            isReloading = true;
            await System.Threading.Tasks.Task.Delay(3000);
            isReloading = false;
            Debug.Log($"Trying to reload ad {placementId}");
            requestAd(placementId);
        }

        public void OnInitializationComplete()
        {
            Debug.Log("Unity Ads initialization complete");

            //MetaData metaData = new MetaData("privacy");
            //metaData.Set("mode", "none");
            //Advertisement.SetMetaData(metaData);
        }

        public async void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogError($"Unity ads initialization failed. Error: {error} message: {message}");

            do
            {
                await System.Threading.Tasks.Task.Delay(3000);
            }
            while (Application.internetReachability == NetworkReachability.NotReachable);

            Debug.Log("Trying to re-init unity ads");
            Initialize();
        }
#endif
    }
}

