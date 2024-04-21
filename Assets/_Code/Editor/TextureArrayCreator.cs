using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Arena.Editor
{
	public class TextureArrayCreator : ScriptableWizard
	{
		[MenuItem("Arena/Утилиты/Создание массива текстур")]
		public static void ShowWindow()
		{
			DisplayWizard<TextureArrayCreator>("Создание массива текстур", "Создать");
		}

		public string filename = "MyTextureArray";

		public List<Texture2D> textures = new();

		private ReorderableList list;

		void OnWizardCreate()
		{
			CreateArray(textures, filename);
		}

		private void CreateArray(List<Texture2D> textures, string filename)
		{
			if(textures == null || textures.Count == 0)
			{
				Debug.LogError("Нет текстур");
				return;
			}

			// Texture2D sample = textures[0];
			// Texture2DArray textureArray = new Texture2DArray(sample.width, sample.height, textures.Count, sample.format, false);
			// textureArray.filterMode = FilterMode.Trilinear;
			// textureArray.wrapMode = TextureWrapMode.Repeat;
			//
			// for (int i = 0; i < textures.Count; i++)
			// {
			// 	Texture2D tex = textures[i];
			// 	textureArray.SetPixels(tex.GetPixels(0), i, 0);
			// }
			// textureArray.Apply();

			var textureArray = TzarGames.Rendering.Baking.LightmapBakingSystem.CreateTextureArray(textures.ToArray());

			var path = EditorUtility.SaveFilePanelInProject("Сохранение", filename, "asset",
				"Выберите путь для сохранения");
			
			AssetDatabase.CreateAsset(textureArray, path);
		}
	}
}