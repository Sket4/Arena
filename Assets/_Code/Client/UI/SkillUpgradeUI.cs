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

        [SerializeField] private TextUI requiredLevel;

        [SerializeField] private TextUI desc;

        public IntCounterUI Counter;

        public Button ActivateButton;
        public GameObject PassiveAbilityLabel;
        
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

        public string RequiredLevel
        {
            get => requiredLevel.text;
            set => requiredLevel.text = value;
        }

        public string Description
        {
            get => desc.text;
            set => desc.text = value;
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

        public Color IconColor
        {
            get => icon.color;
            set => icon.color = value;
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
