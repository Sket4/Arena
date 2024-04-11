//#if ENABLE_FIREBASE

//using TzarGames.Common;
//using UnityEngine;

//namespace TzarGames.GoogleFirebase
//{
//    public static class Crashlytics
//    {
//        static bool initialized = false;

//        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//        static void init () 
//        {
//            if(GoogleFirebaseApp.IsReady)
//            {
//                initService();
//            }
//            else
//            {
//                GoogleFirebaseApp.OnReady += GoogleFirebaseApp_OnReady;
//            }
//        }

//        private static void GoogleFirebaseApp_OnReady()
//        {
//            initService();
//        }

//        static void initService()
//        {
//            Debug.Log("Initializing crashlytics service");
//            Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = Privacy.CanCollectData;
//            Privacy.OnDataCollectionPermissionChanged += Privacy_OnDataCollectionPermissionChanged;
//            //Application.logMessageReceived += Application_logMessageReceived;
//            initialized = true;
//        }

//        //private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
//        //{
//        //    if(initialized == false)
//        //    {
//        //        return;
//        //    }

//        //    Firebase.Crashlytics.Crashlytics.Log(string.Format("{0}: {1}", type, condition));
//        //}

//        private static void Privacy_OnDataCollectionPermissionChanged(bool obj)
//        {
//            Debug.Log("Changing data collection settings for Firebase Crashlytics to " + obj);
//            Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = Privacy.CanCollectData;
//        }
//    }
//}
//#endif