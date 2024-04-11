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
        TextUI commonCounterLabel = default;

        [SerializeField]
        TextUI cooldownCounterLabel = default;

        public IntCounterUI CommonCounter;
        public string CommonCounterLabel
        {
            get
            {
                return commonCounterLabel.text;
            }
            set
            {
                commonCounterLabel.text = value;
            }
        }

        public string CooldownCounterLabel
        {
            get
            {
                return cooldownCounterLabel.text;
            }
            set
            {
                cooldownCounterLabel.text = value;
            }
        }

        public IntCounterUI CooldownCounter;

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
            commonCounterLabel.enabled = false;
            cooldownCounterLabel.enabled = false;
            CommonCounter.enabled = false;
        }

        public void OnPulledFromPool()
        {
            commonCounterLabel.enabled = true;
            cooldownCounterLabel.enabled = true;
            CommonCounter.enabled = true;
        }
    }
}
