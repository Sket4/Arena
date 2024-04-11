using UnityEngine;

namespace Arena.Client.UI
{
    public class NotificationBehaviour : MonoBehaviour
    {
        [SerializeField]
        float tempMessageDefaultTime = 5;

        [SerializeField]
        Sprite defaultSprite = default;

        private NotificationEntryUI entry;
        private NotificationUI lastNotificationUI;

        public void ShowTempLocalizedMessage(TzarGames.Common.LocalizedStringAsset message)
        {
            var notificationUi = FindObjectOfType<NotificationUI>();
            if (notificationUi == null)
            {
                Debug.LogError("Failed to get notification UI");
                return;
            }

            if(defaultSprite == null)
            {
                notificationUi.AddTempNotification(message, tempMessageDefaultTime);
            }
            else
            {
                notificationUi.AddTempNotificationWithIcon(message, defaultSprite, tempMessageDefaultTime);
            }
        }

        public void SetConstantLocalizedMessage(TzarGames.Common.LocalizedStringAsset message)
        {
            SetConstantMessage(message);
        }
        
        public void SetConstantMessage(string message)
        {
            
            if (entry == null)
            {
                bool error = false;
                var notificationUi = FindObjectOfType<NotificationUI>();
                if (notificationUi == null)
                {
                    error = true;
                }
                else
                {
                    lastNotificationUI = notificationUi;
                    entry = notificationUi.AddConstantNotification(message);
                    if (entry == null)
                    {
                        error = true;
                    }
                }

                if (error)
                {
                    Debug.LogError("Failed to set constant message");
                    return;
                }
            }
            else
            {
                entry.Message = message;    
            }
        }

        public void RemoveConstantMessage()
        {
            if (entry == null)
            {
                return;
            }
            
            if (lastNotificationUI == null)
            {
                Debug.LogError("Failed to get notification UI");
                return;
            }

            lastNotificationUI.RemoveConstantNotification(entry);
            entry = null;
            lastNotificationUI = null;
        }
    }
}
