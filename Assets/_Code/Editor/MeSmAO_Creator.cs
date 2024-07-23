using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Arena.Editor
{
	public class MeSmAO_Creator : ScriptableWizard
	{
		public enum FileTypes
		{
			PNG,
			TGA
		}
		
		[MenuItem("Arena/Утилиты/MeSmAO creator _F10")]
		public static void ShowWindow()
		{
			DisplayWizard<MeSmAO_Creator>("Создание текстуры MeSmAO", "Создать");
		}
		public Texture2D MetallicSmoothnessMap;
        public Texture2D CustomSmoothnessMap;
        public bool InvertSmoothness;
		public Texture2D AO_Map;
		public FileTypes FileType;
		public bool IgnoreAlpha = true;

		void OnWizardCreate()
		{
			var mesmPixels = MetallicSmoothnessMap.GetPixels();
			bool useAO = AO_Map != null;
			bool useCustomSmoothness = CustomSmoothnessMap != null;
			Color[] smoothnessPixels = useCustomSmoothness ? CustomSmoothnessMap.GetPixels() : null;
			Color[] aoPixels = useAO ? AO_Map.GetPixels() : null;

			for (var index = 0; index < mesmPixels.Length; index++)
			{
				ref var mesmPixel = ref mesmPixels[index];

				if (useCustomSmoothness)
				{
					mesmPixel.g = smoothnessPixels[index].r;
				}
				else
				{
					mesmPixel.g = mesmPixel.a;
				}

				if (InvertSmoothness)
				{
					mesmPixel.g = 1.0f - mesmPixel.g;
				}

				if (useAO)
				{
					mesmPixel.b = aoPixels[index].r;
				}
				else
				{
					mesmPixel.b = 1;
				}

				if (IgnoreAlpha)
				{
					mesmPixel.a = 1;
				}

				mesmPixel = mesmPixel.linear;
			}

			var newTexture = new Texture2D(MetallicSmoothnessMap.width, MetallicSmoothnessMap.height, TextureFormat.RGBA32, true, true);
			
			newTexture.SetPixels(mesmPixels);
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

			var defaultPath = AssetDatabase.GetAssetPath(MetallicSmoothnessMap);
			defaultPath = Path.GetDirectoryName(defaultPath);
			Debug.Log($"Default path: {defaultPath}");
			
			var path = EditorUtility.SaveFilePanelInProject("Сохранение", $"MeSmAO", extension,
				"Выберите путь для сохранения", defaultPath);
			
			File.WriteAllBytes(path, bytes);
			
			AssetDatabase.ImportAsset(path);
		}
	}
}