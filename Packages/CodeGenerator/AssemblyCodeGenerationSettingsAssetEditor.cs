using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TzarGames.CodeGeneration
{
    [CustomEditor(typeof(AssemblyCodeGenerationSettingsAsset), true)]
    public class AssemblyCodeGenerationSettingsAssetEditor : CodeGenerationSettingsAssetEditor
    {
        private ReorderableList assembliesList;
        private ReorderableList prepareOnlyAssembliesList;

        private void OnEnable()
        {
            assembliesList = createList("Сборки", "Assemblies");
            prepareOnlyAssembliesList = createList("Сборки для подготовки", "PrepareOnlyAssemblies");
        }

        ReorderableList createList(string header, string propertyName)
        {
            var list = new ReorderableList(serializedObject,
                serializedObject.FindProperty(propertyName),
                true, true, true, true);

            list.drawHeaderCallback = (Rect rect) =>
            {
                GUI.Label(rect, header);
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };

            return list;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var asset = (AssemblyCodeGenerationSettingsAsset)target;

            if (string.IsNullOrEmpty(asset.RelativeSavePath))
            {
                return;
            }

            GUILayout.Space(10);

            serializedObject.Update();
            prepareOnlyAssembliesList.DoLayoutList();
            GUILayout.Space(10);
            assembliesList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);
            
            if(GUILayout.Button("Сгенерировать код"))
            {
                CodeGeneratorTools.GenerateAssemblyCode(asset);
            }
            if (GUILayout.Button("Удалить код"))
            {
                CodeGeneratorTools.Fix(asset);
            }
        }
    }
}
