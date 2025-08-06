using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class GraphicsSettingsUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        Toggle shadows = default;

        [SerializeField] private Toggle fpsLimit = default;

        [SerializeField]
        private TMPro.TMP_Dropdown qualityDropdown;

        [SerializeField]
        Toggle colorEnhance = default;

        protected override void OnVisible()
        {
            base.OnVisible();
            qualityDropdown.value = (int)AppSettings.GraphicsSettings.Quality;
            shadows.isOn = AppSettings.GraphicsSettings.Shadows;
            fpsLimit.isOn = AppSettings.GraphicsSettings.FpsLimit;
        }

        public void OnQualityChanged(int val)
        {
            AppSettings.GraphicsSettings.Quality = (AppSettings.QualityLevels)val;
        }

        public void OnShadowEnabledChanged(bool enabled)
        {
            AppSettings.GraphicsSettings.Shadows = enabled;
        }

        public void OnFpsLimitEnabled(bool enabled)
        {
            AppSettings.GraphicsSettings.FpsLimit = enabled;
        }
    }
}
