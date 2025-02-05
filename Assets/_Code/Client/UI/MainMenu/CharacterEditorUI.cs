using System.Collections.Generic;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class CharacterEditorUI : UIBase
	{
		[SerializeField] private CreateCharacterUI createCharacterUi = default;
		[SerializeField] private UIBase waitingWindow = default;
        [SerializeField] private RectTransform listContainer = default;
		[SerializeField] private CharacterEntryUI characterEntryPrefab = default;

		[SerializeField] private Button loadButton = default;
        [SerializeField] private Button deleteButton = default;

        Dictionary<CharacterEntryUI, CharacterData> infos = new();
		private CharacterEntryUI lastSelected = null;

		public System.Action OnCharacterSelectedLoaded;

		protected override void OnVisible()
		{
			base.OnVisible();
			
			refreshList();
		}

		void refreshList()
		{
			foreach (Transform child in listContainer)
			{
				Destroy(child.gameObject);
			}
			infos.Clear();
			lastSelected = null;
			loadButton.interactable = false;
            deleteButton.interactable = false;

            if (GameState.Instance == null || GameState.Instance.PlayerData == null)
            {
	            return;
            }
			var characters = GameState.Instance.PlayerData.Characters;
			
			for (var i = 0; i < characters.Count; i++)
			{
				var characterInfo = characters[i];
				var entry = Instantiate(characterEntryPrefab);
				entry.CharacterName = characterInfo.Name;
				entry.transform.SetParent(listContainer, false);
				entry.Selected = characterInfo.Name.Equals(GameState.Instance.PlayerData.SelectedCharacterName);
				if (entry.Selected)
				{
					lastSelected = entry;
				}

				entry.OnSelected += EntryOnOnSelected;
				infos.Add(entry, characterInfo);
			}
		}

		private void EntryOnOnSelected(CharacterEntryUI characterEntryUi)
		{
			if (lastSelected != null)
			{
				lastSelected.Selected = false;
			}
			lastSelected = characterEntryUi;
			lastSelected.Selected = true;

			if (infos[lastSelected] == GameState.Instance.SelectedCharacter)
			{
				loadButton.interactable = false;
                deleteButton.interactable = false;
			}
			else
			{
				loadButton.interactable = true;
                deleteButton.interactable = true;
            }
		}

		public void CreateCharacter()
		{
			createCharacterUi.SetCancelState(true);
            createCharacterUi.OnGoToNextScene += CreateCharacterUi_OnGoToNextScene;
			createCharacterUi.SetNextMenu(this);
			createCharacterUi.AutoSelectCharacter = false;
			SetVisible(false);
			createCharacterUi.SetVisible(true);
		}

        private void CreateCharacterUi_OnGoToNextScene()
        {
            createCharacterUi.OnGoToNextScene -= CreateCharacterUi_OnGoToNextScene;
            if(createCharacterUi.LastCreatedCharacter != null)
            {
                SetVisible(false);
                loadCharacter(createCharacterUi.LastCreatedCharacter.Name);
            }
        }

        public void LoadSelectedCharacter()
		{
			if (lastSelected == null)
			{
				return;
			}
			
			var character = infos[lastSelected];
            
            if(character == null)
            {
                return;
            }

            loadCharacter(character.Name);
		}

        private async void loadCharacter(string characterName)
        {
	        SetVisible(false);
	        waitingWindow.SetVisible(true);
	        
            await GameState.Instance.SelectCharacter(characterName);
            
            waitingWindow.SetVisible(false);
            
            OnCharacterSelectedLoaded?.Invoke();
        }

        public async void ConfirmDeleteSelectedCharacter()
        {
            if (lastSelected == null)
            {
                return;
            }

            var character = infos[lastSelected];

            if(GameState.Instance.SelectedCharacter == character)
            {
                return;
            }
            
            await GameState.Instance.DeleteCharacter(character.Name);
            refreshList();
        }
	}
}
