#if ENABLE_FIREBASE

using System;
using UnityEngine;

namespace TzarGames.GoogleFirebase
{
    public static class GoogleFirebaseApp
    {
        public static bool IsReady
        {
            get; private set;
        }

        public static event Action OnReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void init()
        {
#if !UNITY_EDITOR
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => 
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp, i.e.
                    //   app = Firebase.FirebaseApp.DefaultInstance;
                    // where app is a Firebase.FirebaseApp property of your application class.

                    // Set a flag here indicating that Firebase is ready to use by your
                    // application.
                    IsReady = true;

                    if(OnReady != null)
                    {
                        OnReady();
                    }
                }
                else
                {
                    Debug.LogError(string.Format(
                      "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                    // Firebase Unity SDK is not safe to use here.
                }
            });
#endif
        }
    }
}
#endif
