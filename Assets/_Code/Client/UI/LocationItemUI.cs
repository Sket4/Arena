using System;
using Arena.Quests;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
	public class LocationItemUI : MonoBehaviour
	{
		[SerializeField] private TextUI text;
		[SerializeField] private Image activeImage;

		[NonSerialized] public LocationClientData ClientData;
		[NonSerialized] public Sprite Icon;
		public event System.Action<LocationItemUI> OnClicked; 

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
