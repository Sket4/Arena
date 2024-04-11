using System.Collections;
using TzarGames.Common;
using UnityEngine;

namespace Arena.Client.UI.MainMenu
{
    public class PrivacyUI : MonoBehaviour
    {
        [SerializeField]
        TzarGames.Common.UI.UIBase mainWindow = default;

        [SerializeField]
        string nextSceneName = default;

        [SerializeField]
        float nextSceneLoadDelay = 1;

        [SerializeField]
        LocalizedStringAsset privacyPolicyUrl = default;

        [SerializeField]
        GameObject loadingMessage = default;

        [SerializeField]
        GameObject fadeScreen = default;

        private void Start()
        {
            if(Privacy.PrivacyAnswerGiven)
            {
#if UNITY_ANDROID
                StartCoroutine(loadScene(false));
#else
                StartCoroutine(loadScene(true));
                fadeScreen.SetActive(true);
#endif
            }
            else
            {
                mainWindow.SetVisible(true);
            }
        }

        public void Accept()
        {
            Privacy.CanCollectData = true;
            Privacy.PrivacyAnswerGiven = true;
            StartCoroutine(loadScene(true));
        }

        public void Decline()
        {
            Privacy.CanCollectData = false;
            Privacy.PrivacyAnswerGiven = true;
            StartCoroutine(loadScene(true));
        }

        IEnumerator loadScene(bool withDelay)
        {
            loadingMessage.SetActive(true);
            if(withDelay)
            {
                yield return new WaitForSeconds(nextSceneLoadDelay);
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(nextSceneName);
        }

        public void OpenPrivacyPolicy()
        {
            Application.OpenURL(privacyPolicyUrl);
        }
    }
}
