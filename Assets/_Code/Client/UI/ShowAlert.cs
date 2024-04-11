using TzarGames.Common;
using UnityEngine;

namespace Arena.Client.UI
{
    public class ShowAlert : MonoBehaviour
	{
		[SerializeField] private string _message = default;
		[SerializeField] private LocalizedStringAsset localizedMessage = default;
		[SerializeField] private bool useGlobalUI = default;

		public void ShowLocalizedMessage(LocalizedStringAsset message)
		{
			Show(localizedMessage);
		}
		
		public void Show(string message)
		{
			if (useGlobalUI == false)
			{
				var ui = FindObjectOfType<GameUI>();
				ui.ShowAlert(message);	
			}
			else
			{
				var ui = GlobalUI.Instance;
				if (ui != null)
				{
					ui.Alert.Show(message);	
				}
			}
		}

		public void Show()
		{
			if (localizedMessage == null)
			{
				Show(_message);	
			}
			else
			{
				Show(localizedMessage);
			}
		}
	}	
}
