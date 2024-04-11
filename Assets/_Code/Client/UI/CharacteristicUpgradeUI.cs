using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class CharacteristicUpgradeUI : MonoBehaviour
    {
        [SerializeField]
        LocalizedStringAsset templateText = default;

        [SerializeField]
        IntCounterUI counter = default;

        [SerializeField]
        TextUI label = default;

        public string TemplateText
        {
            get
            {
                return templateText;
            }
        }

        public IntCounterUI Counter
        {
            get
            {
                return counter;
            }
        }

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

        void Reset()
        {
            counter = GetComponentInChildren<IntCounterUI>();

            var texts = GetComponentsInChildren<TextUI>();
            TextUI first = null;
            foreach(var text in texts)
            {
                if(first == null)
                {
                    first = text;
                }
                if(text.LocalizedString != null)
                {
                    label = text;
                    templateText = label.LocalizedString;
                    return;
                }
            }

            if(first != null)
            {
                label = first;
            }
        }
    }
}
