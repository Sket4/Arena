using UnityEngine;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class GraphicsSettingsUI : TzarGames.Common.UI.UIBase
    {
        [SerializeField]
        Toggle shadows = default;

        [SerializeField]
        private TMPro.TMP_Dropdown qualityDropdown;

        [SerializeField]
        Toggle colorEnhance = default;

        protected override void OnVisible()
        {
            base.OnVisible();
            qualityDropdown.value = (int)AppSettings.GraphicsSettings.Quality;
        }

        public void OnQualityChanged(int val)
        {
            AppSettings.GraphicsSettings.Quality = (AppSettings.QualityLevels)val;
        }
    }
}
