using UnityEngine;
using UnityEditor;

namespace Arena.Tests
{
    [InitializeOnLoad]
    public static class TestEditorTools
    {
        const string launchMatchServerMenuPath = "Arena/Tests/Lauch match server";
        const string launchServerMenuPath = "Arena/Tests/Lauch server";
        const string launchClientMenuPath = "Arena/Tests/Lauch client";
        const string authClientMenuPath = "Arena/Tests/Authenticate client";

        static TestEditorTools()
        {
            /// Delaying until first editor tick so that the menu
            /// will be populated before setting check state, and
            /// re-apply correct action
            EditorApplication.delayCall += () => {
                init();
            };
        }

        static void init()
        {
            Menu.SetChecked(launchServerMenuPath, TestServerGameLauncher.LaunchTestServer);
            Menu.SetChecked(launchClientMenuPath, TestClientGameLauncher.LaunchTestClient);
            Menu.SetChecked(authClientMenuPath, TestClientGameLauncher.AuthorizeTestClient);
            Menu.SetChecked(launchMatchServerMenuPath, TestServerGameLauncher.LaunchMatchServer);
        }
        
        [MenuItem(launchMatchServerMenuPath)]
        static void toggleLaunchMatchServer()
        {
            TestServerGameLauncher.LaunchMatchServer = !TestServerGameLauncher.LaunchMatchServer;
            Menu.SetChecked(launchMatchServerMenuPath, TestServerGameLauncher.LaunchMatchServer);
            Debug.LogFormat("LaunchMatchServer set to {0}", TestServerGameLauncher.LaunchMatchServer);
        }

        [MenuItem(launchServerMenuPath)]
        static void toggleLaunchServer()
        {
            TestServerGameLauncher.LaunchTestServer = !TestServerGameLauncher.LaunchTestServer;
            Menu.SetChecked(launchServerMenuPath, TestServerGameLauncher.LaunchTestServer);
            Debug.LogFormat("LaunchTestServer set to {0}", TestServerGameLauncher.LaunchTestServer);
        }

        [MenuItem(launchClientMenuPath)]
        static void toggleLaunchClient()
        {
            TestClientGameLauncher.LaunchTestClient = !TestClientGameLauncher.LaunchTestClient;
            Menu.SetChecked(launchClientMenuPath, TestClientGameLauncher.LaunchTestClient);
            Debug.LogFormat("LaunchTestClient set to {0}", TestClientGameLauncher.LaunchTestClient);
        }

        [MenuItem(authClientMenuPath)]
        static void toggleAuthClient()
        {
            TestClientGameLauncher.AuthorizeTestClient = !TestClientGameLauncher.AuthorizeTestClient;
            Menu.SetChecked(authClientMenuPath, TestClientGameLauncher.AuthorizeTestClient);
            Debug.LogFormat("AuthorizeTestClient set to {0}", TestClientGameLauncher.AuthorizeTestClient);
        }
    }
}

