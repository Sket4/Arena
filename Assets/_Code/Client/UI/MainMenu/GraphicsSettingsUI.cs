using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class GraphicsSettingsUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        Toggle lowQuality = default;

        [SerializeField]
        Toggle shadows = default;

        [SerializeField]
        Toggle colorEnhance = default;

        protected override void OnVisible()
        {
            base.OnVisible();

            lowQuality.isOn = AppSettings.GraphicsSettings.LowQuality;
        }

        public void OnLowQualityChanged(bool value)
        {
            AppSettings.GraphicsSettings.LowQuality = value;
        }
    }
}
