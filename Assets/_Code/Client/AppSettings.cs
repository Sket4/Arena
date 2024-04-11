using UnityEngine;
using TzarGames.Common;

namespace Arena
{
    public class AppSettings 
	{
		private const string SFX_VOLUME_KEY = "SFX_VOLUME_KEY";
		private const string MUSIC_VOLUME_KEY = "MUSIC_VOLUME_KEY";

        private const string GAME_ALLOW_MULTIPLAYER_IN_LOBBY = "GAME_ALLOW_MULTIPLAYER_IN_LOBBY";

        public static float SfxVolume
		{
			get
			{
				return PlayerPrefs.GetFloat (SFX_VOLUME_KEY, 1.0f);
			}
			set 
			{
				PlayerPrefs.SetFloat (SFX_VOLUME_KEY, value);
			}
		}

		public static float MusicVolume
		{
			get
			{
				return PlayerPrefs.GetFloat (MUSIC_VOLUME_KEY, 1.0f);
			}
			set 
			{
				PlayerPrefs.SetFloat (MUSIC_VOLUME_KEY, value);
			}
		}

        public static bool AllowMultiplayerInLobby
        {
            get
            {
                return PlayerPrefs.GetInt(GAME_ALLOW_MULTIPLAYER_IN_LOBBY, 1) > 0;
            }
            set
            {
                PlayerPrefs.SetInt(GAME_ALLOW_MULTIPLAYER_IN_LOBBY, value ? 1 : 0);
            }
        }

        [ConsoleCommand]
		static void setVsyncCount(int mode)
		{
			QualitySettings.vSyncCount = mode;
		}
		
		[ConsoleCommand]
		static void setTargetFramerate(int frameRate)
		{
			Debug.Log($"Setting target framerate to {frameRate}");
			Application.targetFrameRate = frameRate;
		}

        public static class GraphicsSettings
        {
            private const string DEFAULT_QUALITY = "TOTAL_GRAPHICS_DEFAULT_QUALITY";
            private const string LOW_QUALITY = "TOTAL_GRAPHICS_LOW_QUALITY";
            private const string SHADOWS = "TOTAL_GRAPHICS_SHADOWS";
            private const string COLOR_ENHANCE = "TOTAL_GRAPHICS_COLOR_ENHANCE";

            public static bool LowQuality
            {
                get
                {
                    return PlayerPrefs.GetInt(LOW_QUALITY, 0) > 0;
                }
                set
                {
                    PlayerPrefs.SetInt(LOW_QUALITY, value ? 1 : 0);
                    Adjust();
                }
            }

            public static bool Shadows
            {
                get
                {
                    return PlayerPrefs.GetInt(SHADOWS, 1) > 0;
                }
                set
                {
                    PlayerPrefs.SetInt(SHADOWS, value ? 1 : 0);
                    Adjust();
                }
            }

            public static bool ColorEnhance
            {
                get
                {
                    return PlayerPrefs.GetInt(COLOR_ENHANCE, 1) > 0;
                }
                set
                {
                    PlayerPrefs.SetInt(COLOR_ENHANCE, value ? 1 : 0);
                    Adjust();
                }
            }

            public static void Adjust()
            {
                if(PlayerPrefs.HasKey(DEFAULT_QUALITY) == false)
                {
                    PlayerPrefs.SetInt(DEFAULT_QUALITY, QualitySettings.GetQualityLevel());
                }

                if(LowQuality)
                {
                    Debug.Log("Setting low quality settings");
                    QualitySettings.SetQualityLevel(0);
                }
                else
                {
                    Debug.Log("Setting default quality settings");
                    QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(DEFAULT_QUALITY));
                }
            }
        }
	}
}
