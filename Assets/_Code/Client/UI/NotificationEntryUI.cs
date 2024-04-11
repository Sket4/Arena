using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class NotificationEntryUI : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
        [SerializeField] private TextUI text = default;
        [SerializeField] private Image iconImage = default;
        
        public string Message 
        {
            get { return text.text; }
            set { text.text = value; }    
        }

        public Sprite Icon
        {
            get
            {
                if(iconImage == null)
                {
                    return null;
                }
                return iconImage.sprite;
            }
            set
            {
                if(iconImage == null)
                {
                    return;
                }
                iconImage.sprite = value;

                if(value == null)
                {
                    iconImage.gameObject.SetActive(false);
                }
                else
                {
                    iconImage.gameObject.SetActive(true);
                }
            }
        }

        public void OnPulledFromPool()
        {
            text.enabled = true;
            if(iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
            }
        }

        public void OnPushedToPool()
        {
            text.enabled = false;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.gameObject.SetActive(false);
            }
        }
    }
}
