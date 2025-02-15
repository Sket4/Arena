// Copyright 2012-2017 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.

using System;
using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Arena.Client.UI.MainMenu
{
    public class SettingsUI : UIBase
    {
        [SerializeField] private TextUI version = default;

        [Serializable]
        struct CategoryTab
        {
            public Button Button;
            public UIBase Window;
        }

        Sprite defaultCategoryTabSprite;
        [SerializeField] private Sprite activeCategoryTabSprite;
        
        [SerializeField] private CategoryTab[] categoryTabs;
        [SerializeField] private GameObject socialAuthButton = default;
        [SerializeField] private GameObject restorePurchasesButton = default;

        [SerializeField]
        Slider sfxSlider = default;

        [SerializeField]
        Slider musicSlider = default;

        private float lastSfxVolume = 0;
        private float lastMusicVolume = 0;

        [SerializeField] private AudioMixer musicMixer = default;
        [SerializeField] private AudioMixer sfxMixer = default;

        [SerializeField] private float mixerMinValue = -80;

        [SerializeField] private float checkUpdateInterval = 0.2f;

        [SerializeField] Toggle lobbyMultiplayerToggle = default;
		
        private float lastCheckTime = 0;

        [SerializeField] string privacyPolicyUrl = default;

        private CategoryTab activeCategoryTab;

        private void Update()
        {
            if (Time.time - lastCheckTime >= checkUpdateInterval)
            {
                lastCheckTime = Time.time;
                check();
            }
        }

        float getMixerValue(float volume)
        {
            return -((1.0f - volume) * mixerMinValue);
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            activateCategory(categoryTabs[0]);
        }

        void check()
        {
            if (Mathf.Abs(lastSfxVolume - sfxSlider.value) > FMath.KINDA_SMALL_NUMBER)
            {
                lastSfxVolume = sfxSlider.value;
                AppSettings.SfxVolume = lastSfxVolume;
                sfxMixer.SetFloat("volume", getMixerValue(lastSfxVolume));
            }
			
            if (Mathf.Abs(lastMusicVolume - musicSlider.value) > FMath.KINDA_SMALL_NUMBER)
            {
                lastMusicVolume = musicSlider.value;
                AppSettings.MusicVolume = lastMusicVolume;
                musicMixer.SetFloat("volume", getMixerValue(lastMusicVolume));
            }
        }

        void activateCategory(CategoryTab categoryTab)
        {
            if (activeCategoryTab.Button == categoryTab.Button)
            {
                return;
            }
            categoryTab.Button.image.sprite = activeCategoryTabSprite;
            categoryTab.Window.gameObject.SetActive(true);
            categoryTab.Window.SetVisible(true);

            foreach (var tab in categoryTabs)
            {
                if (tab.Button == categoryTab.Button)
                {
                    continue;
                }
                tab.Button.image.sprite = defaultCategoryTabSprite;
                if (tab.Window.IsVisible)
                {
                    tab.Window.gameObject.SetActive(false);
                }
            }
        }
        
        protected override void Start()
        {
            base.Start();

            defaultCategoryTabSprite = categoryTabs[0].Button.image.sprite;
            
            foreach (var categoryTab in categoryTabs)
            {
                categoryTab.Button.onClick.AddListener(() =>
                {
                    activateCategory(categoryTab);
                });
            }

            if(GameState.Instance != null)
                version.text = GameState.Instance.Version;

            //if (SocialSystem.Instance.CanSignOut == false)
            //{
            //    socialAuthButton.SetActive(false);
            //}

            Debug.LogError("restorePurchasesButton.SetActive(UnityInAppManager.SupportsRestoringPurchases);");
            //restorePurchasesButton.SetActive(UnityInAppManager.SupportsRestoringPurchases);
            
            sfxSlider.value = AppSettings.SfxVolume;
            musicSlider.value = AppSettings.MusicVolume;
			
            lastMusicVolume = musicSlider.value;
            lastSfxVolume = sfxSlider.value;
            
            if(musicMixer != null)
                musicMixer.SetFloat("volume", getMixerValue(lastMusicVolume));
                
            sfxMixer.SetFloat("volume", getMixerValue(lastSfxVolume));

            if (lobbyMultiplayerToggle)
            {
                lobbyMultiplayerToggle.isOn = AppSettings.AllowMultiplayerInLobby;    
            }
            else
            {
                Debug.LogError("lobbyMultiplayerToggle is null");
            }
        }

        public void OnLobbyMultiplayerToggleChanged(bool on)
        {
            AppSettings.AllowMultiplayerInLobby = on;
        }

        public void RestorePurchases()
        {
            throw new System.NotImplementedException();
            //UnityInAppManager.Instance.RestorePurchases();
        }

        public void FacebookLogin()
        {
            throw new System.NotImplementedException();
            //if(FacebookPlatform.IsLoggedIn == false)
            //{
            //    FacebookPlatform.Login(null);
            //}
        }

        public void OpenPrivacyPolicy()
        {
            Application.OpenURL(privacyPolicyUrl);
        }
    }
}
