using UnityEngine;
using UnityEditor;

namespace Arena.Editor
{
    // Временное решение для автоматизации переноса коллайдеров на Subscene
    public class CollisionMover : EditorWindow
    {
        [SerializeField]
        Transform source;

        [SerializeField]
        Transform destination;

        [MenuItem("Arena/Утилиты/Перенос коллизий")]
        static void show()
        {
            var window = EditorWindow.CreateInstance<CollisionMover>();
            window.Show();
        }

        void OnGUI()
        {
            source = EditorGUILayout.ObjectField(source, typeof(Transform), true) as Transform;
            destination = EditorGUILayout.ObjectField(destination, typeof(Transform), true) as Transform;

            if(GUILayout.Button("Скопировать"))
            {
                destroyChilds(destination);
                copyColliderRecurse(source);
            }

            if(source != null && GUILayout.Button("Включить все коллайдеры"))
            {
                enableColldiers();
            }
        }

        void enableColldiers()
        {
            var colldiers = source.GetComponentsInChildren<Collider>();
            foreach(var c in colldiers)
            {
                c.enabled = true;
                EditorUtility.SetDirty(c);
            }
        }

        void destroyChilds(Transform transform)
        {
            var cnt = transform.childCount;

            for(int i=cnt-1; i>=0; i--)
            {
                var tr = transform.GetChild(i);
                DestroyImmediate(tr.gameObject);
            }
        }

        void copyColliderRecurse(Transform root)
        {
            foreach(Transform tr in root)
            {
                copyColliderRecurse(tr);
            }

            var colliders = root.GetComponents<Collider>();

            if(colliders == null || colliders.Length == 0)
            {
                return;
            }

            foreach(var c in colliders)
            {
                c.enabled = true;
            }

            var instance = Instantiate(root.gameObject, root.transform.position, root.transform.rotation);
            
            instance.transform.SetParent(destination);
            instance.name = root.name;
            instance.tag = "Untagged";
            instance.isStatic = true;

            var parent = root.parent;
            Vector3 scale = root.localScale;

            while(parent != null)
            {
                var parentScale = parent.localScale;
                scale.x *= parentScale.x;
                scale.y *= parentScale.y;
                scale.z *= parentScale.z;

                parent = parent.parent;
            }

            instance.transform.localScale = scale;

            Debug.Log($"scales: {root.transform.lossyScale} and instance {instance.transform.lossyScale}");

            destroyChilds(instance.transform);

            foreach(var c in colliders)
            {
                c.enabled = false;
            }
        }
    }
}
