using TzarGames.Common.UI;
using TzarGames.MatchFramework.Client;
using UnityEngine;

namespace Arena.Client.UI
{   
    public class UIConnecting : MonoBehaviour
    {
        [SerializeField]
        TMPro.TextMeshProUGUI statusText;

        [SerializeField]
        UIBase waitingWindow;

        [SerializeField]
        UIBase failedWindow;

        [SerializeField]
        UIBase authWithEmailAndPassWindow;

        [Header("Auth by e-mail")]
        [SerializeField]
        TMPro.TMP_InputField emailInput;
        [SerializeField]
        TMPro.TMP_InputField passwordInput;

        const string emailKey = "UI_AUTH_EMAIL";
        const string passwordKey = "UI_AUTH_PASSWORD";

        UIBase[] windows;

        private void Awake()
        {
            emailInput.text = PlayerPrefs.GetString(emailKey, "");
            passwordInput.text = PlayerPrefs.GetString(passwordKey, "");

            windows = GetComponentsInChildren<UIBase>(true);
        }

        private async void OnEnable()
        {
            GameState.Connecting.OnConnectionFailed += GameState_OnConnectionFailed;
            GameState.Connecting.OnConnectionStateChanged += Connecting_OnConnectionStateChanged;

            showWindow(null);

            await System.Threading.Tasks.Task.Yield();

            if(enabled == false)
            {
                return;
            }
            updateStatus(GameState.Connecting.State);
        }

        private void OnDisable()
        {
            GameState.Connecting.OnConnectionFailed -= GameState_OnConnectionFailed;
            GameState.Connecting.OnConnectionStateChanged -= Connecting_OnConnectionStateChanged;
        }

        void showWindow(UIBase window)
        {
            foreach(var otherWindow in windows)
            {
                if(window == otherWindow)
                {
                    if(otherWindow.IsVisible == false)
                        otherWindow.SetVisible(true);
                }
                else
                {
                    if (otherWindow.IsVisible)
                        otherWindow.SetVisible(false);
                }
            }
        }

        private void Connecting_OnConnectionStateChanged(GameState.Connecting.ConnectingState state)
        {
            updateStatus(state);
        }

        void updateStatus(GameState.Connecting.ConnectingState state)
        {
            switch (state)
            {
                case GameState.Connecting.ConnectingState.WaitingForCredentials:
                    setConnectingStatus("");
                    showWindow(authWithEmailAndPassWindow);
                    break;
                case GameState.Connecting.ConnectingState.Authorizing:
                    setConnectingStatus("Authorizing...");
                    showWindow(waitingWindow);
                    break;
                case GameState.Connecting.ConnectingState.LoadingPlayerData:
                    setConnectingStatus("Loading player data...");
                    showWindow(waitingWindow);
                    break;
                case GameState.Connecting.ConnectingState.Failed:
                    showWindow(failedWindow);
                    break;
                case GameState.Connecting.ConnectingState.Finished:
                    setConnectingStatus("Loading menu...");
                    showWindow(waitingWindow);
                    break;
            }
        }

        void setConnectingStatus(string status, bool error = false)
        {
            statusText.text = status;
            if(error)
            {
                statusText.color = Color.red;
            }
            else
            {
                statusText.color = Color.white;
            }
        }

        private void GameState_OnConnectionFailed(GameState.Connecting.ConnectionError obj)
        {
            setConnectingStatus($"Error: {obj}", true);
            showWindow(failedWindow);
        }

        public void Reconnect()
        {
            if(GameState.Connecting.State == GameState.Connecting.ConnectingState.WaitingForCredentials)
            {
                setConnectingStatus("");
                showWindow(authWithEmailAndPassWindow);
                return;
            }
            showWindow(waitingWindow);
            setConnectingStatus("Connecting...");

            GameState.Instance.Reconnect();
        }

        public void AuthorizeByEmail()
        {
            saveCredentials();
            GameState.Connecting.ContinueWithCredentials(emailInput.text, passwordInput.text);
        }

        void saveCredentials()
        {
            PlayerPrefs.SetString(emailKey, emailInput.text);
            PlayerPrefs.SetString(passwordKey, passwordInput.text);
        }

        public async void CreateAccountByEmail()
        {
            setConnectingStatus("Creating user account...");
            showWindow(waitingWindow);

            try
            {
                var result = await Authentication.FirebaseCreateUserWithEmailAndPassword(emailInput.text, passwordInput.text);
                if (result)
                {
                    setConnectingStatus("Account created succesfully");
                    saveCredentials();
                    //await Task.Delay(2000);
                    showWindow(authWithEmailAndPassWindow);
                }
                else
                {
                    setConnectingStatus("Failed to create user account", true);
                    //await Task.Delay(2000);
                    showWindow(authWithEmailAndPassWindow);
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
                updateStatus(GameState.Connecting.ConnectingState.Failed);
            }
        }
    }
}
