using UnityEditor;
using UnityEngine;

namespace TzarGames.CodeGeneration
{
    [CustomEditor(typeof(CodeGenerationSettingsAsset), true)]
    public class CodeGenerationSettingsAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = target as CodeGenerationSettingsAsset;

            GUILayout.Label($"Папка для сохранения: {asset.RelativeSavePath}");

            GUILayout.Space(5);

            if (GUILayout.Button("Указать папку для сохранения"))
            {
                SelectSavePath(asset);
            }
        }

        private static void SelectSavePath(CodeGenerationSettingsAsset asset)
        {
            var path = EditorUtility.OpenFolderPanel("Указать папку для сохранения сгенерированного кода", "", "");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = path.Replace(Application.dataPath, "");
            asset.RelativeSavePath = path;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static void CheckAndCreateSaveDirectory(CodeGenerationSettingsAsset asset)
        {
            var path = Application.dataPath + asset.RelativeSavePath;

            if(System.IO.Directory.Exists(path) == false)
            {
                System.IO.Directory.CreateDirectory(path);
            }
        }
    }
}
