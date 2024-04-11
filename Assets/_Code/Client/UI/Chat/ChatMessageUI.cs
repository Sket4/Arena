using Unity.Burst.Intrinsics;
using UnityEngine;

namespace Arena.Client.UI.Chat
{
    public class ChatMessageUI : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
        [SerializeField]
        TzarGames.Common.UI.TextUI messageText = default;

        const string messageFormat = "<color=#{0}>{1}:</color> {2}";

        public string PlayerName
        {
            get; private set;
        }
        
        public void Build(string playerName, ref Color playerNameColor, string message)
        {
            PlayerName = playerName;
            var colorHex = ColorUtility.ToHtmlStringRGBA(playerNameColor);
            var final = string.Format(messageFormat, colorHex, playerName, message);
            messageText.text = final;
        }

        public void OnPushedToPool()
        {
            PlayerName = null;
            messageText.enabled = false;
        }

        public void OnPulledFromPool()
        {
            messageText.enabled = true;
        }
    }
}
