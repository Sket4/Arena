using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

namespace Arena.Editor
{
	public class MeSm_Creator : ScriptableWizard
	{
		public enum FileTypes
		{
			PNG,
			TGA
		}
		
		[MenuItem("Arena/Утилиты/MeSm creator _F10")]
		public static void ShowWindow()
		{
			DisplayWizard<MeSm_Creator>("Создание текстуры MeSm", "Создать");
		}
		public Texture2D MetallicMap;
		public bool UseMetallicChannel = false;
		[Range(0,3)]
		public byte MetallicChannel = 0;
        public Texture2D SmoothnessMap;
        public bool InvertSmoothness;
        [Range(0,3)]
        public byte SmoothnessChannel = 0;
        [Range(0,1)]
        public float MetallicScale = 1;
        public float SmoothnessScale = 1;
		public FileTypes FileType;

		void OnWizardCreate()
		{
			Color[] destPixels;
			string defaultPath;

			if (MetallicMap)
			{
				destPixels = MetallicMap.GetPixels();	
				defaultPath = AssetDatabase.GetAssetPath(MetallicMap);
				Color[] smoothnessPixels = null;

				if (SmoothnessMap)
				{
					smoothnessPixels = SmoothnessMap.GetPixels();
				}
				
				for (var index = 0; index < destPixels.Length; index++)
				{
					ref var mesmPixel = ref destPixels[index];
					
					var sm = mesmPixel[SmoothnessChannel]; 

					if(UseMetallicChannel)
					{
						var met = mesmPixel[MetallicChannel]; 
						mesmPixel.r = met;
						mesmPixel.g = met;
						mesmPixel.b = met;
					}
					mesmPixel.r *= MetallicScale;
					mesmPixel.g *= MetallicScale;
					mesmPixel.b *= MetallicScale;

					if (smoothnessPixels != null)
					{
						var smPixel = smoothnessPixels[index];
						mesmPixel.a = smPixel[SmoothnessChannel];
					}
					else
					{
						mesmPixel.a = sm;
					}

					if (InvertSmoothness)
					{
						mesmPixel.a = 1.0f - mesmPixel.a;
					}

					mesmPixel.a = math.saturate(mesmPixel.a * SmoothnessScale);

					mesmPixel = mesmPixel.linear;
				}
			}
			else
			{
				destPixels = SmoothnessMap.GetPixels();	
				defaultPath = AssetDatabase.GetAssetPath(SmoothnessMap);
				
				for (var index = 0; index < destPixels.Length; index++)
				{
					ref var mesmPixel = ref destPixels[index];

					mesmPixel.a = destPixels[index][SmoothnessChannel];
					
					mesmPixel.r = MetallicScale;
					mesmPixel.g = MetallicScale;
					mesmPixel.b = MetallicScale;

					if (InvertSmoothness)
					{
						mesmPixel.a = 1.0f - mesmPixel.a;
					}
					mesmPixel.a = math.saturate(mesmPixel.a * SmoothnessScale);

					mesmPixel = mesmPixel.linear;
				}
			}
			
			Vector2Int size = default;

			if (MetallicMap)
			{
				size.x = MetallicMap.width;
				size.y = MetallicMap.height;
			}
			else
			{
				size.x = SmoothnessMap.width;
				size.y = SmoothnessMap.height;
			}

			var newTexture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, true, true);
			
			newTexture.SetPixels(destPixels);
			newTexture.Apply();

			byte[] bytes;
			string extension;

			switch (FileType)
			{
				case FileTypes.PNG:
					bytes = newTexture.EncodeToPNG();
					extension = "png";
					break;
				case FileTypes.TGA:
					bytes = newTexture.EncodeToTGA();
					extension = "tga";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			defaultPath = Path.GetDirectoryName(defaultPath);
			Debug.Log($"Default path: {defaultPath}");
			
			var path = EditorUtility.SaveFilePanelInProject("Сохранение", $"MeSm", extension,
				"Выберите путь для сохранения", defaultPath);
			
			File.WriteAllBytes(path, bytes);
			
			AssetDatabase.ImportAsset(path);
		}
	}
}