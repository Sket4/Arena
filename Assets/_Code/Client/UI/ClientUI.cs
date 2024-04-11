using System.Collections;
using TzarGames.Common.UI;
using TzarGames.MultiplayerKit.Client;
using UnityEngine;

namespace Arena.Client.UI
{
    public class ClientUI : MonoBehaviour
    {
        [SerializeField]
        ClientLauncher launcher;

        [SerializeField]
        Canvas canvas;

        [SerializeField]
        UnityEngine.Events.UnityEvent onCanvasEnabled;

        [SerializeField]
        UnityEngine.Events.UnityEvent onCanvasDisabled;

        [SerializeField]
        UIBase connectingWindow;

        [SerializeField]
        UIBase errorWindow;

        bool exiting = false;

        void Update()
        {
            if(exiting)
            {
                return;
            }
            
            if ((launcher.GameLoop as GameClient).ClientSystem.TryGetConnectionState(out ClientConnectionState connectionState) == false)
            {
                return;
            }

            switch (connectionState.State)
            {
                case ClientConnectionStates.Connecting:
                    if(connectingWindow.IsVisible == false)
                    {
                        connectingWindow.SetVisible(true);
                    }
                    if(errorWindow.IsVisible)
                    {
                        errorWindow.SetVisible(false);
                    }
                    break;
                
                case ClientConnectionStates.Disconnected:
                    if (connectionState.IsFailedToConnect 
                        || (connectionState.WasDisconnectedFromServer 
                            && (GameState.Instance.IsConnectingToGameServer == false && GameState.Instance.IsLoadingScene == false)))
                    {
                        if (errorWindow.IsVisible == false)
                        {
                            Debug.Log($"Opening error window, failed to connect: {connectionState.IsFailedToConnect}, " +
                                      $"was disconnected: {connectionState.WasDisconnectedFromServer}");
                            errorWindow.SetVisible(true);
                        }    
                    }
                    
                    if (connectingWindow.IsVisible)
                    {
                        connectingWindow.SetVisible(false);
                    }
                    break;
                case ClientConnectionStates.Connected:
                    if (errorWindow.IsVisible)
                    {
                        errorWindow.SetVisible(false);
                    }
                    if (connectingWindow.IsVisible)
                    {
                        connectingWindow.SetVisible(false);
                    }
                    break;
            }

            if(connectionState.State == ClientConnectionStates.Connected)
            {
                if(canvas.enabled)
                {
                    canvas.enabled = false;
                    onCanvasDisabled.Invoke();
                }
            }
            else
            {
                if (canvas.enabled == false)
                {
                    canvas.enabled = true;
                    onCanvasEnabled.Invoke();
                }
            }
        }

        public void Exit()
        {
            exiting = true;
            StartCoroutine(exit());
        }

        IEnumerator exit()
        {
            connectingWindow.SetVisible(false);
            errorWindow.SetVisible(false);
            yield return new WaitForSeconds(1);
            GameState.Instance.ExitToMainMenu();
        }
    }
}
