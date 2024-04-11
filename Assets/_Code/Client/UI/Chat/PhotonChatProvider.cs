using UnityEngine;
//using ExitGames.Client.Photon;
//using Photon.Chat;

namespace Arena.Client.UI.Chat
{
    class PhotonChatProvider //: Photon.Chat.IChatClientListener, IChatProvider
    {
        const string appVersion = "1.0";
		//List<IChatClientListener> _listeners;
		//ChatClient chatClient;

        public string UserID
        {
            get; private set;
        }

		private PhotonChatProvider()
		{
			//_listeners = new List<IChatClientListener>();
            //chatClient = new ChatClient(this);
		}

        static PhotonChatProvider _instance = null;

        public static PhotonChatProvider Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new PhotonChatProvider();
                }
                return _instance;
            }
        }

        public string ActiveChannel
        {
            get; set;
        }

        //public void AddListener(IChatClientListener listener)
        //{
        //    if(_listeners.Contains(listener))
        //    {
        //        return;
        //    }
        //    _listeners.Add(listener);
        //}

        //public void RemoveListener(IChatClientListener listener)
        //{
        //    if (_listeners.Contains(listener) == false)
        //    {
        //        return;
        //    }
        //    _listeners.Remove(listener);
        //}

        public void SubscribeToChannels(string[] channels)
        {
            Debug.LogError("Not implemented");
            //chatClient.Subscribe(channels);
        }

        public void UpdateState()
        {
            Debug.LogError("Not implemented");
            //chatClient.Service();
        }

        public void Connect(string userId)
        {
            Debug.LogError("Not implemented");
            //if(chatClient.State == ChatState.Disconnected || chatClient.State == ChatState.Uninitialized)
            //{
            //    UserID = userId;
            //    chatClient.ChatRegion = "EU";
            //    chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID, appVersion, new Photon.Chat.AuthenticationValues(userId));
            //}
        }

        public void SendMessageToChannel(string channel, string message)
        {
            Debug.LogError("Not implemented");
            //chatClient.PublishMessage(channel, message);
        }

        public void Disconnect()
        {
            Debug.LogError("Not implemented");
            //chatClient.Disconnect();
        }

		//public TzarChatState GetState()
		//{
  //          var state = chatClient.State;

		//	if(state == ChatState.ConnectedToFrontEnd)
  //          {
  //              return TzarChatState.Connected;
  //          }
  //          if(state == ChatState.Disconnected || state == ChatState.Uninitialized)
  //          {
  //              return TzarChatState.Disconnected;
  //          }
  //          if(state == ChatState.Disconnecting)
  //          {
  //              return TzarChatState.Disconnecting;
  //          }
  //          return TzarChatState.Connecting;
		//}

        //public void DebugReturn(DebugLevel level, string message)
        //{
        //    //throw new System.NotImplementedException();
        //}

        //public void OnChatStateChange(ChatState state)
        //{
        //    //throw new System.NotImplementedException();
        //}

        public void OnConnected()
        {
            Debug.LogError("Not implemented");
            //foreach(var listener in _listeners)
            //{
            //    listener.OnConnected();
            //}            
        }

        public void OnDisconnected()
        {
            UserID = null;
            Debug.LogError("Not implemented");
            //foreach (var listener in _listeners)
            //{
            //    listener.OnDisconnected();
            //}
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            Debug.LogError("Not implemented");
            //foreach (var listener in _listeners)
            //{
            //    listener.OnGetMessages(channelName, senders, messages);
            //}
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            //throw new System.NotImplementedException();
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
            //throw new System.NotImplementedException();
        }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            //throw new System.NotImplementedException();
        }

        public void OnUnsubscribed(string[] channels)
        {
            //throw new System.NotImplementedException();
        }

        public void OnUserSubscribed(string channel, string user)
        {
            //
        }

        public void OnUserUnsubscribed(string channel, string user)
        {
            //
        }
    }
}