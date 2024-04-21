using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

namespace Arena.Editor
{
	public class TerrainSplatmapConverter : ScriptableWizard
	{
		[MenuItem("Arena/Утилиты/Конвертация Terrain Splatmap")]
		public static void ShowWindow()
		{
			DisplayWizard<TerrainSplatmapConverter>("Конвертация Terrain Splatmap", "Запуск");
		}

		public string filename = "converted_splatmap";

		public Texture2D texture;

		//private ReorderableList list;

		void OnWizardCreate()
		{
			ConvertSplatmap(texture, filename);
		}

		public static void ConvertSplatmap(Texture2D splatmapTexture, string filename)
		{
			if(splatmapTexture == null)
			{
				Debug.LogError("Нет текстуры");
				return;
			}

			var pixels = splatmapTexture.GetPixels();

			for (var index = 0; index < pixels.Length; index++)
			{
				ref var pixel = ref pixels[index];

				float max = float.MinValue;
				int firstMaxIndex = 0;
				
				for (int i = 0; i < 4; i++)
				{
					if (pixel[i] > max)
					{
						max = pixel[i];
						firstMaxIndex = i;
					}
				}

				int secondMaxIndex = -1;
				max = float.MinValue;
				
				for (int i = 0; i < 4; i++)
				{
					if (i == firstMaxIndex)
					{
						continue;
					}
					
					if (pixel[i] > max)
					{
						max = pixel[i];
						secondMaxIndex = i;
					}
				}

				var indexA = firstMaxIndex;
				var indexB = secondMaxIndex;

				float blendA = pixel[indexA];
				float blendB = pixel[indexB];

				float diff = 1.0f - (blendA + blendB);
				blendA += diff / 2;
				float indexDiff = indexA - indexB;

				if(true)
				{
					pixel.r = (indexDiff + 4) / 8;
					pixel.g = blendA;
					pixel.b = indexA / 4.0f;
				
					pixel.a = indexB / 4.0f;	
				}
				else
				{
					pixel.r = (indexDiff + 4) / 8;
					pixel.g = math.min(indexA, indexB);
				}
				
				
				//Debug.Log(pixel);
			}

			var newTexture = new Texture2D(splatmapTexture.width, splatmapTexture.height);
			newTexture.SetPixels(pixels);
			newTexture.Apply();
			
			var path = EditorUtility.SaveFilePanelInProject("Сохранение", filename, "tga",
				"Выберите путь для сохранения");

			var bytes = newTexture.EncodeToTGA();
			File.WriteAllBytes(path, bytes);
			AssetDatabase.ImportAsset(path);
		}
	}
}