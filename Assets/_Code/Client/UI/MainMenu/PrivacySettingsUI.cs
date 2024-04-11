using TzarGames.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class PrivacySettingsUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        LocalizedStringAsset privacyPolicyPage = default;

        [SerializeField]
        Toggle enableDataCollection = default;
        

        protected override void OnVisible()
        {
            base.OnVisible();

            enableDataCollection.isOn = TzarGames.Common.Privacy.CanCollectData;
        }

        public void OnAllowDataCollectChanged(bool value)
        {
            TzarGames.Common.Privacy.CanCollectData = value;
        }

        public void OpenPrivacyPolicyPage()
        {
            Application.OpenURL(privacyPolicyPage);
        }
    }
}
