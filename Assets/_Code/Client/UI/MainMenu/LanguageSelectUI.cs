using System.Collections.Generic;
using UnityEngine;
using I2.Loc;
using TMPro;
using TzarGames.Common;

namespace Arena.Client.UI.MainMenu
{
    public class LanguageSelectUI : MonoBehaviour
	{
		[SerializeField] private LanguageSourceAsset source = default;
		[SerializeField] private TMP_Dropdown list = default;
		
		void Start ()
		{
			var langs = source.SourceData.mLanguages;
			var listOfLangs = new List<string>(langs.Count);

			for (var index = 0; index < langs.Count; index++)
			{
				var languageData = langs[index];
				listOfLangs.Add(languageData.Name);
			}
			
			list.AddOptions(listOfLangs);
			list.value = source.mSource.GetLanguageIndex(LocalizationManager.CurrentLanguage);
			list.onValueChanged.AddListener(onOtherLanguageSelected);
		}

		private void onOtherLanguageSelected(int arg0)
		{
			LocalizedStringAsset.SetLanguage(list.options[arg0].text);
		}
	}	
}
