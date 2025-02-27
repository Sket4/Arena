using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI
{
    public class PlayClick : MonoBehaviour
    {
        private Button button;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(GameUI.PlayClick);
        }
    }
}
