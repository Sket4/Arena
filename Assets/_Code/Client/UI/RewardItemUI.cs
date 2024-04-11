using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class RewardItemUI : MonoBehaviour
    {
        [SerializeField] private TextUI itemName = default;
        [SerializeField] private Image icon = default;

        public string Label
        {
            get { return itemName.text; }
            set { itemName.text = value; }
        }

        public Sprite Icon
        {
            get { return icon.sprite; }
            set { icon.sprite = value; }
        }
    }    
}
