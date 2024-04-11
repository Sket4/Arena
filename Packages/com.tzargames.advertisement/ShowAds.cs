// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using UnityEngine;

namespace TzarGames.Ads
{
    public enum ShowResult
    {
        Failed,
        Skipped,
        Finished,
    }
    
    public interface IAdsService
    {
        bool IsReady(Ad ad);
        void Show(Ad ad, Action<ShowResult> callback);
        void Initialize();
    }
    
    [System.Serializable]
    class AdInfo
    {
        public int showCount = 0;
        public int attemtCount = 0;
    }

    public class ShowAds : MonoBehaviour
    {
        public Ad Ad;
        
        [Tooltip("Display ads across the specified number of times (1 - each time)")]
        public int DisplayAcrossTimes = 1;
        
        [Tooltip("Display ads after the specified number of attempts (0 - play immediately)")]
        public int DisplayAfter = 0;
        
        public bool OpenOnce = false;
        
        public bool ShowOnEnable = false;
        public bool ShowOnStart = false;
        public bool ShowOnDisable = false;
        
        [SerializeField, HideInInspector]
        private string guid = null;
        
        private const string AD_PREFIX = "AD_INFO_";
        
        private AdInfo currentAdInfo;
        
        //bool pendingCloseAd = false;
        
        public UnityEngine.Events.UnityEvent OnAdFinished;
        public UnityEngine.Events.UnityEvent OnAdSkipped;
        public UnityEngine.Events.UnityEvent OnAdFailed;

        void Reset()
        {
            if(string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
                Debug.Log("Unique GUID for ad: " + guid);
            }
            
            var newAdInfo = new AdInfo();
            saveAdInfo(newAdInfo);
        }
        
        void Awake()
        {
            var key = getAdKey();
            if(PlayerPrefs.HasKey(AD_PREFIX + guid) == false)
            {
                var newAdInfo = new AdInfo();
                saveAdInfo(newAdInfo);
            }
            
            var serialized = PlayerPrefs.GetString(key);
            currentAdInfo = UnityEngine.JsonUtility.FromJson<AdInfo>(serialized);
        }
        
        void Start()
        {
            if(ShowOnStart)
            {
                ShowAd();
            }
        }
        
        string getAdKey()
        {
            return AD_PREFIX + guid;
        }
        
        void saveAdInfo(AdInfo info)
        {
            var serialized = UnityEngine.JsonUtility.ToJson(info);
            Debug.Log("Saving ad info: " + serialized);
            PlayerPrefs.SetString(getAdKey(), serialized);
        }
        
        void OnEnable()
        {
            if(ShowOnEnable)
            {
                ShowAd();
            }
        }
        
        void OnDisable()
        {
            if(ShowOnDisable)
            {
                ShowAd();
            }
        }

        private IAdsService getService()
        {
            return AdsServiceManager.GetReadyServiceForAd(Ad);
        }

        public void RequestAd()
        {
            AdsServiceManager.RequestAdServicesForAd(Ad);
        }

        public bool IsReady()
        {
            var service = getService();
            if (service != null)
            {
                return service.IsReady(Ad);    
            }
            return false;
        }
        
        public void ShowAd()
        {
            if(OpenOnce && currentAdInfo.showCount > 0)
            {
                Debug.Log("Skip ad show, OpenOnce is true");
                OnShowAdResult(ShowResult.Finished);
                return;
            }
            
            var service = getService();
            
            if (service != null)
            {
                if(currentAdInfo.attemtCount >= DisplayAfter)
                {
                    if( DisplayAcrossTimes > 1 && (currentAdInfo.showCount % DisplayAcrossTimes != 0)
                    )
                    {
                        Debug.Log("Skip ad display: DisplayAcrossTimes" + DisplayAcrossTimes + " Show count:" + currentAdInfo.showCount);
                    }
                    else
                    {
                        Debug.Log("Display Ad: " + Ad.name + " DisplayAcrossTimes" + DisplayAcrossTimes + " Show count:" + currentAdInfo.showCount);

                        service.Show(Ad, OnShowAdResult);
                        
                        //pendingCloseAd = true;
                    }
                    
                    currentAdInfo.showCount++;
                }
                
                currentAdInfo.attemtCount++;
                saveAdInfo(currentAdInfo);
            }
            else
            {
                Debug.LogWarning("Advertisement is not ready: " + Ad.name);
            }
        }

        void OnShowAdResult(ShowResult result)
        {
            Debug.Log ("Advert show result: " + result);
            
            switch (result)
            {
            case ShowResult.Finished:
                OnAdFinished.Invoke();
                break;
            case ShowResult.Skipped:
                OnAdSkipped.Invoke();
                break;
            case ShowResult.Failed:
                OnAdFailed.Invoke();
                break;
            }
        }
    }
}
