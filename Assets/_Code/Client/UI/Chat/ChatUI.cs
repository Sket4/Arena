using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena.Client.UI.Chat
{
    public class ChatUI : StateMachine//, IChatClientListener
	{
		[SerializeField]
		TzarGames.Common.UI.UIBase chatWindow = default;

        [SerializeField]
        TzarGames.Common.UI.UIBase settingsWindow = default;

		[SerializeField]
		UnityEngine.UI.ScrollRect scroll = default;

		[SerializeField]
		int maxMessageCount = 50;

		[SerializeField]
		ChatMessageUI messagePrefab = default;

		[SerializeField]
		RectTransform messageContainer = default;

        [SerializeField]
        Color otherNameColor = Color.cyan;

        [SerializeField]
        Color playerNameColor = Color.blue;
        
		[SerializeField]
		TzarGames.Common.UI.InputFieldUI inputField = default;

        [SerializeField]
        RectTransform currentUsersContainer = default;

        [SerializeField]
        RectTransform blockedUserContainer = default;

        [SerializeField]
        ChatUserEntryUI userEntryUI = default;


        List<string> blockedUsers = new List<string>();

		//IChatProvider chatProvider;

        [System.Serializable]
        class ChatData
        {
            public List<string> BlockedUsers;
        }

        const string CHAT_DATA_KEY = "CHAT_DATA_KEY";

        TzarGames.Common.InstancePool<ChatMessageUI> messagePool;
		List<ChatMessageUI> activeMessages = new List<ChatMessageUI>();

        class ChatState : State
        {
            public ChatUI UI
            {
                get
                {
                    return Owner as ChatUI;
                }
            }
        }

        [DefaultState]
        class Chat : ChatState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.chatWindow.SetVisible(true);
            }
            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.chatWindow.SetVisible(false);
            }

            public override void Update()
            {
                base.Update();

                var ui = UI;

                Debug.LogError("Not implemented");
                //if (ui.chatProvider.GetState() != TzarChatState.Connected)
                //{
                //    if (ui.chatWindow.IsVisible)
                //    {
                //        ui.chatWindow.SetVisible(false);
                //    }
                //    return;
                //}

                if (ui.chatWindow.IsVisible == false)
                {
                    ui.chatWindow.SetVisible(true);
                }
            }
        }

        class Settings : ChatState
        {
            public override void OnStateBegin(State prevState)
            {
                base.OnStateBegin(prevState);
                UI.settingsWindow.SetVisible(true);
                updateUserList();
            }

            public override void OnStateEnd(State nextState)
            {
                base.OnStateEnd(nextState);
                UI.settingsWindow.SetVisible(false);
            }

            void updateUserList()
            {
                var ui = UI;

                foreach (Transform child in ui.blockedUserContainer)
                {
	                Destroy(child.gameObject);
                }
                foreach (Transform child in ui.currentUsersContainer)
                {
	                Destroy(child.gameObject);
                }

                var playerNames = new List<string>();

                foreach(var message in ui.activeMessages)
                {
                    Debug.LogError("Not implemented");
                    //if (message.PlayerName == ui.chatProvider.UserID)
                    //{
                    //    continue;
                    //}

                    if (playerNames.Contains(message.PlayerName) == false)
                    {
                        playerNames.Add(message.PlayerName);
                    }
                }

                for (int i = playerNames.Count-1; i >= 0; i--)
                {
                    var playerName = playerNames[i];

                    if(ui.blockedUsers.Contains(playerName))
                    {
                        continue;
                    }

                    var entryInstance = Instantiate(ui.userEntryUI);
                    entryInstance.UserName = playerName;
                    entryInstance.IsBlocked = false;
                    entryInstance.transform.SetParent(ui.currentUsersContainer);
                    entryInstance.transform.localScale = Vector3.one;
                    entryInstance.OnTogglePressed += EntryInstance_OnTogglePressed;
                }

                foreach (var blockedUser in ui.blockedUsers)
                {
                    var entryInstance = Instantiate(ui.userEntryUI);
                    entryInstance.UserName = blockedUser;
                    entryInstance.IsBlocked = true;
                    entryInstance.transform.SetParent(ui.blockedUserContainer);
                    entryInstance.transform.localScale = Vector3.one;
                    entryInstance.OnTogglePressed += EntryInstance_OnTogglePressed;
                }
            }

            void EntryInstance_OnTogglePressed(ChatUserEntryUI obj)
            {
                bool changed = false;

                if(obj.IsBlocked)
                {
                    if(UI.blockedUsers.Contains(obj.UserName) == false)
                    {
                        UI.blockedUsers.Add(obj.UserName);
                        changed = true;
                    }
                }
                else
                {
                    if (UI.blockedUsers.Contains(obj.UserName))
                    {
                        UI.blockedUsers.Remove(obj.UserName);
                        changed = true;
                    }
                }

                if(changed)
                {
                    if(GameState.Instance != null)
                    {
	                    Debug.LogError("Not implemented");
                        // var commonData = GameState.Instance.CommonSaveGameData;
                        // var serializedData = commonData.GetString(CHAT_DATA_KEY);
                        //
                        // ChatData chatData;
                        //
                        // if (serializedData != null)
                        // {
                        //     try
                        //     {
                        //         chatData = JsonUtility.FromJson<ChatData>(serializedData);
                        //     }
                        //     catch (System.Exception e)
                        //     {
                        //         Debug.LogException(e);
                        //         chatData = new ChatData();
                        //     }
                        // }
                        // else
                        // {
                        //     chatData = new ChatData();
                        // }
                        //
                        // chatData.BlockedUsers = UI.blockedUsers;
                        //
                        // var json = JsonUtility.ToJson(chatData);
                        // commonData.SetString(CHAT_DATA_KEY, json);
                        //
                        // if (GameState.Instance.IsItSafeStateToSaveGame())
                        // {
                        //     GameState.Instance.SaveGame();
                        // }
                    }

                    updateUserList();
                }
            }
        }

		ChatMessageUI createMessage(ChatMessageUI prefab)
		{
			var instance = Instantiate(prefab);
			instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
			return instance;
		}

        public void CloseSettings()
        {
            GotoState<Chat>();
        }

        public void ShowSettings()
        {
            GotoState<Settings>();
        }
        
		public void Send()
		{
            if(string.IsNullOrEmpty(inputField.Text))
            {
                return;
            }

            Debug.LogError("Not implemented");
            //chatProvider.SendMessageToChannel(chatProvider.ActiveChannel, inputField.Text);
            inputField.Text = "";
		}

		void Start()
		{
            if(GameState.Instance != null)
            {
                Debug.LogError("Not implemented");
                // var commonData = GameState.Instance.CommonSaveGameData;
                // var serializedData = commonData.GetString(CHAT_DATA_KEY);
                // if(serializedData != null)
                // {
                //     try
                //     {
                //         var chatData = JsonUtility.FromJson<ChatData>(serializedData);
                //         blockedUsers = chatData.BlockedUsers;
                //     }
                //     catch(System.Exception e)
                //     {
                //         Debug.LogException(e);
                //     }
                // }
            }

            Debug.LogError("Not implemented");
            //chatProvider = PhotonChatProvider.Instance;
            //chatProvider.AddListener(this);
            messagePool = new TzarGames.Common.InstancePool<ChatMessageUI>(messagePrefab, createMessage, maxMessageCount);
			messagePool.CreateObjects(50);
		}

        void OnDestroy()
        {
            Debug.LogError("Not implemented");
            //if(chatProvider != null)
            //{
            //    chatProvider.RemoveListener(this);
            //}
        }

        public void OnConnected()
        {
        }

        public void OnDisconnected()
        {
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
			//Debug.Log("scroll pos: " + scroll.verticalNormalizedPosition);
			bool scrolToEnd = scroll.verticalNormalizedPosition < 0.001f;

			for(int i=0; i<messages.Length; i++)
			{
				var m = messages[i];
				var s = senders[i];

				//Debug.LogFormat("Message {0} : {1}", s, m);
                if(blockedUsers != null && blockedUsers.Contains(s))
                {
#if UNITY_EDITOR
                    Debug.Log("Skipping the messaage from blocked user: " + s);
#endif
                    continue;
                }

				ChatMessageUI ui;
				if(activeMessages.Count >= maxMessageCount)
				{
					ui = activeMessages[0];
					activeMessages.RemoveAt(0);
				}
				else
				{
					ui = messagePool.Get();
				}

				activeMessages.Add(ui);
				
				ui.gameObject.hideFlags = HideFlags.None;
                Color nameColor;
                Debug.LogError("Not implemented");
                //if(s.Equals(chatProvider.UserID))
                //{
                //    nameColor = playerNameColor;
                //}
                //else
                {
                    nameColor = otherNameColor;
                }
                ui.Build(s, ref nameColor, (string)m);

				var tr = ui.transform;
				tr.SetParent(messageContainer);
				tr.localScale = Vector3.one;
				tr.SetAsLastSibling();
			}

			if(scrolToEnd)
			{
				StartCoroutine(scrollRoutine());
			}
        }

		IEnumerator scrollRoutine()
		{
			yield return null;
			scroll.verticalNormalizedPosition = 0;
		}

        void Update()
		{
            CurrentState.Update();
		}
	}
}
