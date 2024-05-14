using System;
using UnityEngine;
using TzarGames.Common;
using UnityEngine.Rendering;

namespace Arena
{
    public class AppSettings 
	{
		public enum QualityLevels
		{
			Low = 0,
			Medium = 1,
			High = 2
		}

		private const string LOW_SHADER_QUALITY = "UG_QUALITY_LOW";
		private const string MEDIUM_SHADER_QUALITY = "UG_QUALITY_MED";
		private const string HIGH_SHADER_QUALITY = "UG_QUALITY_HIGH";
		
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
            private const string QUALITY = "TOTAL_GRAPHICS_QUALITY";
            private const string SHADOWS = "TOTAL_GRAPHICS_SHADOWS";
            private const string COLOR_ENHANCE = "TOTAL_GRAPHICS_COLOR_ENHANCE";

            [ConsoleCommand]
            public static void SetLowQuality()
            {
	            Quality = QualityLevels.Low;
            }
            [ConsoleCommand]
            public static void SetMediumQuality()
            {
	            Quality = QualityLevels.Medium;
            }
            
            [ConsoleCommand]
            public static void SetHighQuality()
            {
	            Quality = QualityLevels.High;
            }

            public static QualityLevels Quality
            {
                get
                {
	                if (PlayerPrefs.HasKey(QUALITY))
	                {
		                return (QualityLevels)PlayerPrefs.GetInt(QUALITY);    
	                }

	                var quality = QualityLevels.Medium;

	                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
	                {
		                quality = QualityLevels.Low;
	                }
	                
                    PlayerPrefs.SetInt(QUALITY, (int)quality);
                    return quality;
                }
                set
                {
                    PlayerPrefs.SetInt(QUALITY, (int)value);
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
                var quality = Quality;

                switch (quality)
                {
	                case QualityLevels.Low:
		                Shader.EnableKeyword(LOW_SHADER_QUALITY);
		                Shader.DisableKeyword(MEDIUM_SHADER_QUALITY);
		                Shader.DisableKeyword(HIGH_SHADER_QUALITY);
		                break;
	                case QualityLevels.Medium:
		                Shader.DisableKeyword(LOW_SHADER_QUALITY);
		                Shader.EnableKeyword(MEDIUM_SHADER_QUALITY);
		                Shader.DisableKeyword(HIGH_SHADER_QUALITY);
		                break;
	                case QualityLevels.High:
		                Shader.DisableKeyword(LOW_SHADER_QUALITY);
		                Shader.EnableKeyword(MEDIUM_SHADER_QUALITY);
		                Shader.DisableKeyword(HIGH_SHADER_QUALITY);
		                break;
	                default:
		                throw new ArgumentOutOfRangeException();
                }
                
                QualitySettings.SetQualityLevel((int)quality);
            }
        }
	}
}
