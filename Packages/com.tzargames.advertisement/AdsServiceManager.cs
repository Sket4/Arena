using System;
using System.Collections.Generic;
using UnityEngine;

namespace TzarGames.Ads
{
    public class AdsServiceManager : MonoBehaviour
    {
        [Header("Сервисы будут использоваться в порядке согласно")]
        [Header("этому списку (первый - самый приоритетный)")]
        [SerializeField]
        List<AdService> _services = default;

        public static AdsServiceManager Instance { get; private set; }
        public event Action<IAdsService> OnAdStarted;
        public event Action<IAdsService> OnAdSkippedOrFinished;

        public static IAdsService GetReadyServiceForAd(Ad ad)
        {
            if (Instance == null)
            {
                return null;
            }
            
            for (var index = 0; index < Instance._services.Count; index++)
            {
                var adService = Instance._services[index];
                if (adService.IsReady(ad))
                {
                    return adService;
                }
            }
            return null;
        }
        
        public static void RequestAdServicesForAd(Ad ad)
        {
            if (Instance == null)
            {
                return;
            }
            
            for (var index = 0; index < Instance._services.Count; index++)
            {
                var adService = Instance._services[index];
                if (adService.IsReady(ad))
                {
                    break;
                }
                else
                {
                    adService.RequestAd(ad);   
                }
            }
        }

        protected virtual void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            for (var index = 0; index < _services.Count; index++)
            {
                var adService = _services[index];
                adService.OnAdStarted += ServiceOnOnAdStarted;
                adService.OnAdSkippedOrFinished += ServiceOnOnAdSkippedOrFinished;
                
                try
                {
                    adService.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                for (var index = 0; index < _services.Count; index++)
                {
                    var adService = _services[index];
                    adService.OnAdStarted -= ServiceOnOnAdStarted;
                    adService.OnAdSkippedOrFinished -= ServiceOnOnAdSkippedOrFinished;
                }
            }
        }

        public void AddService(AdService service)
        {
            if (_services.Contains(service))
            {
                return;
            }
            
            service.OnAdStarted += ServiceOnOnAdStarted;
            service.OnAdSkippedOrFinished += ServiceOnOnAdSkippedOrFinished;
            _services.Add(service);
        }

        public void RemoveService(AdService service)
        {
            if (_services.Contains(service) == false)
            {
                return;
            }
            
            _services.Remove(service);
            service.OnAdStarted -= ServiceOnOnAdStarted;
            service.OnAdSkippedOrFinished -= ServiceOnOnAdSkippedOrFinished;
        }
        
        private void ServiceOnOnAdSkippedOrFinished(IAdsService adsService)
        {
            if (OnAdSkippedOrFinished != null) OnAdSkippedOrFinished(adsService);
        }

        private void ServiceOnOnAdStarted(IAdsService adsService)
        {
            if (OnAdStarted != null) OnAdStarted(adsService);
        }
    }
}
