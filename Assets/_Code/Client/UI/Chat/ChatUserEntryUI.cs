using UnityEngine;

namespace Arena.Client.UI.Chat
{
    public class ChatUserEntryUI : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
        [SerializeField]
        TzarGames.Common.UI.TextUI userName = default;

        [SerializeField]
        UnityEngine.UI.Toggle toggle = default;

        public event System.Action<ChatUserEntryUI> OnTogglePressed;

        public string UserName
        {
            get
            {
                return userName.text;
            }
            set
            {
                userName.text = value;
            }
        }

        public bool IsBlocked
        {
            get
            {
                return toggle.isOn;
            }
            set
            {
                toggle.isOn = value;
            }
        }

        public void OnStateChanged(bool val)
        {
            if(OnTogglePressed != null)
            {
                OnTogglePressed(this);
            }
        }


        public void OnPushedToPool()
        {
            userName.enabled = false;
        }

        public void OnPulledFromPool()
        {
            userName.enabled = true;
        }
    }
}
