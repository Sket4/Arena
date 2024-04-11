#if ENABLE_FIREBASE
#if TZAR_ANALYTICS
using System.Collections;
using System.Collections.Generic;
using TzarGames.Common;
using UnityEngine;
using TzarGames.Common.Analytics;
using Firebase.Analytics;

namespace TzarGames.GoogleFirebase
{
    public class GoogleAnalyticsService : MonoBehaviour, IAnalyticsService
    {
        IEnumerator Start () 
        {
            while (GoogleFirebaseApp.IsReady == false)
            {
                yield return null;
            }

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(Privacy.CanCollectData);
            Privacy.OnDataCollectionPermissionChanged += Privacy_OnDataCollectionPermissionChanged;
            
            

            Debug.Log("Google Analytics service init OK");
            AnalyticsManager.AddService(this);
        }

        private void Privacy_OnDataCollectionPermissionChanged(bool obj)
        {
            Debug.Log("Changing data collection settings for Firebase Analytics to " + obj);
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(Privacy.CanCollectData);
        }

        private void OnDestroy()
        {
            AnalyticsManager.RemoveService(this);
        }

        public void SendEvent(CustomAnalyticsEvent customEvent, Dictionary<string, string> paramaters)
        {
            var fireParameters = new Firebase.Analytics.Parameter[paramaters.Count];
            int index = 0;

            foreach(var p in paramaters)
            {
                var newParam = new Firebase.Analytics.Parameter(p.Key, p.Value);
                fireParameters[index] = newParam;
                index++;
            }

            FirebaseAnalytics.LogEvent(customEvent.name, fireParameters);
        }

        public void SetEnabled(bool enabled)
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
        }
    }
}
#endif
#endif