using System;
using TzarGames.Common.UI;
using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class CharacterEntryUI : MonoBehaviour
    {
        [SerializeField] private TextUI characterName = default;
        [SerializeField] private CanvasRenderer selectedIcon = default;
        private bool selected = false;
        
        public string CharacterName
        {
            get { return characterName.text; }
            set { characterName.text = value; }
        }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                selectedIcon.SetAlpha(value ? 1 : 0);
            }
        }

        public event Action<CharacterEntryUI> OnSelected;

        public void NotifyClicked()
        {
            if (OnSelected != null)
            {
                OnSelected(this);
            }
        }
    }
}
