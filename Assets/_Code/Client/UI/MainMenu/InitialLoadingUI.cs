using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class InitialLoadingUI : MonoBehaviour
    {
        [SerializeField]
        GameObject screenFader = default;

        void Start()
        {
#if UNITY_ANDROID
            screenFader.SetActive(false);
#endif
        }
    }
}
