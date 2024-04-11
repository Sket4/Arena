using UnityEngine;

namespace TzarGames.Ads
{
    public class AdsHelperBehaviour : MonoBehaviour
    {
        [SerializeField]
        Ad[] androidAds;

        [SerializeField]
        Ad[] iosAds;

        [SerializeField]
        bool requestOnStart = true;

        void Start()
        {
            if(requestOnStart)
            {
                foreach(var ad in getAds())
                {
                    Debug.Log($"Requesting ad {ad.AdId}");
                    AdsServiceManager.RequestAdServicesForAd(ad);
                }
            }
        }

        Ad[] getAds()
        {
#if UNITY_IOS
            return iosAds;
#else
            return androidAds;
#endif
        }
    }
}
