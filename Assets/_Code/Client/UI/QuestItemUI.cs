using System;
using Arena.Quests;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
	public class QuestItemUI : MonoBehaviour
	{
		[SerializeField] private TextUI text;
		[SerializeField] private Image activeImage;

		[NonSerialized] public QuestClientData ClientData;
		public event System.Action<QuestItemUI> OnClicked; 

		public void NotifyClicked()
		{
			if (OnClicked != null)
			{
				OnClicked(this);
			}
		}
		
		public string Label
		{
			get { return text.text; }
			set { text.text = value; }
		}

		public bool Activated
		{
			get { return activeImage.gameObject.activeSelf; }
			set
			{
				activeImage.gameObject.SetActive(value);
			}
		}
	}	
}
