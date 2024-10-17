using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arena.Editor
{
    public static class CopyHierarchyTransformTool
    {
        struct TransformInfo
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;

            public TransformInfo(Transform transform)
            {
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
            }
        }

        private static Dictionary<string, TransformInfo> copiedTransforms = new();
        
        [MenuItem("Arena/Утилиты/Копировать иерархию выбранного объекта")]
        static void copy()
        {
            copiedTransforms.Clear();
            
            var childs = Selection.activeGameObject.GetComponentsInChildren<Transform>(true);
            
            foreach (var child in childs)
            {
                if (copiedTransforms.ContainsKey(child.name))
                {
                    continue;
                }
                copiedTransforms.Add(child.name, new TransformInfo(child));
            }
        }
        
        [MenuItem("Arena/Утилиты/Вставить иерархию в выбранный объект")]
        static void paste()
        {
            var childs = Selection.activeGameObject.GetComponentsInChildren<Transform>(true);

            foreach (var info in copiedTransforms)
            {
                foreach (var child in childs)
                {
                    if (info.Key.Equals(child.name))
                    {
                        child.localPosition = info.Value.LocalPosition;
                        child.localRotation = info.Value.LocalRotation;
                        EditorUtility.SetDirty(child);
                        break;
                    }
                }    
            }
        }
    }
}
