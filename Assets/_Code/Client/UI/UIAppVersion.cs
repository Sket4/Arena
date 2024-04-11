using UnityEngine;

namespace Arena.Client.UI
{
    public class UIAppVersion : MonoBehaviour
    {
        [SerializeField]
        TMPro.TextMeshProUGUI text;

        private void Start()
        {
            text.text = Version;
        }

		public string Version
		{
			get
			{
				return GameState.Instance.Version;
			}
		}
	}
}
