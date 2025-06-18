using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class SkillUpgradeUI : MonoBehaviour, TzarGames.GameFramework.IPoolable
    {
        [SerializeField]
        Image icon = default;

        [SerializeField]
        TextUI label = default;

        public IntCounterUI Counter;

        public Button ActivateButton;
        
        public string Label
        {
            get
            {
                return label.text;
            }
            set
            {
                label.text = value;
            }
        }

        public Sprite Icon 
        {
            get
            {
                return icon.sprite;
            }
            set
            {
                icon.sprite = value;
            }
        }

        public void OnPushedToPool()
        {
            icon.sprite = null;
            label.enabled = false;
            Counter.enabled = false;
        }

        public void OnPulledFromPool()
        {
            label.enabled = true;
            Counter.enabled = true;
        }
    }
}
