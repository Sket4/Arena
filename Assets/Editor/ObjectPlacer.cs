using UnityEditor;
using UnityEngine;

namespace Arena.Editor
{
    public class ObjectPlacer : EditorWindow
    {
        GameObject targetMesh;
        GameObject targetPrefab;
        GameObject targetParent;
        float fixedInterval = 1;
        bool reverse = false;
        float offset = 0;
        Vector3 worldSpaceOffset;
        Vector3 additionalRotation;
        Quaternion additionalRotationQ;
        float normalOffset = 0;

        [MenuItem("Arena/Утилиты/Расставление объектов")]
        static void show()
        {
            var window = CreateWindow<ObjectPlacer>();
            window.Show();
        }

        Vector3[] calculateNormals(Vector3[] verts, Transform tr)
        {
            Vector3[] normals = new Vector3[verts.Length];

            for (int v = 0; v < verts.Length; v++)
            {
                Vector3 normal;

                if (v == 0)
                {
                    var vertex = tr.TransformPoint(verts[0]);
                    var nextVertex = tr.TransformPoint(verts[1]);
                    var dir = (nextVertex - vertex).normalized;
                    normal = Vector3.Cross(Vector3.up, dir);
                }
                else if (v == verts.Length - 1)
                {
                    var nextVertex = tr.TransformPoint(verts[verts.Length - 1]);
                    var vertex = tr.TransformPoint(verts[verts.Length - 2]);
                    var dir = (nextVertex - vertex).normalized;
                    normal = Vector3.Cross(Vector3.up, dir);
                }
                else
                {
                    var prevVertex = tr.TransformPoint(verts[v - 1]);
                    var vertex = tr.TransformPoint(verts[v]);
                    var nextVertex = tr.TransformPoint(verts[v + 1]);


                    var dir1 = (vertex - prevVertex).normalized;
                    var normal1 = Vector3.Cross(Vector3.up, dir1);

                    var dir2 = (nextVertex - vertex).normalized;
                    var normal2 = Vector3.Cross(Vector3.up, dir2);

                    normal = (normal1 + normal2).normalized;
                }

                normals[v] = normal;
                //Debug.DrawRay(tr.TransformPoint(verts[v]), normal * 3, Color.blue, 30);
            }
            return normals;
        }

        private void OnGUI()
        {
            targetMesh = EditorGUILayout.ObjectField("Исходный меш", targetMesh, typeof(GameObject), true) as GameObject;
            targetPrefab = EditorGUILayout.ObjectField("Префаб", targetPrefab, typeof(GameObject), true) as GameObject;
            targetParent = EditorGUILayout.ObjectField("Объект-контейнер", targetParent, typeof(GameObject), true) as GameObject;

            reverse = EditorGUILayout.Toggle("Реверс", reverse);
            worldSpaceOffset = EditorGUILayout.Vector3Field("Смещение в мировых координатах", worldSpaceOffset);
            var newRot = EditorGUILayout.Vector3Field("Доп. поворот (углы)", additionalRotation);

            if(newRot != additionalRotation)
            {
                additionalRotation = newRot;
            }

            additionalRotationQ = Quaternion.Euler(additionalRotation);

            normalOffset = EditorGUILayout.FloatField("Смещение по нормали", normalOffset);

            if (GUILayout.Button("Расставить объекты по точкам исходного мешa"))
            {
                var tr = targetMesh.transform;
                var mf = targetMesh.GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                var verts = mesh.vertices;
                Transform targetParentTransform = targetParent != null ? targetParent.transform : null;
                var normals = calculateNormals(verts, tr);

                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 v = verts[i];
                    var position = tr.TransformPoint(v);
                    instantiate(position, normals[i], targetParentTransform);
                }
            }

            GUILayout.Space(20);
            fixedInterval = EditorGUILayout.FloatField("Фиксированный интервал", fixedInterval);
            offset = EditorGUILayout.FloatField("Смещение", offset);

            if (GUILayout.Button("Расставить объекты по точкам исходного мешa через фиксированный интервал"))
            {
                var tr = targetMesh.transform;
                var mf = targetMesh.GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                var verts = mesh.vertices;
                var normals = calculateNormals(verts, tr);
                Transform targetParentTransform = targetParent != null ? targetParent.transform : null;

                
                int currentIndex = 0;



                //bool[] processed = new bool[verts.Length];

                //for(int v=0; v<verts.Length-1; v++)
                //{
                //    var vertex = verts[v];
                //    processed[v] = true;

                //    float minDistance = float.MaxValue;
                //    int closestVertexIndex = -1;

                //    for(int c=0; c<verts.Length; c++)
                //    {
                //        if(c == v)
                //        {
                //            continue;
                //        }
                //        if (processed[c])
                //        {
                //            continue;
                //        }
                //        var otherVertex = verts[c];

                //        var sqrDist = (otherVertex - vertex).sqrMagnitude;

                //        if(sqrDist < minDistance)
                //        {
                //            minDistance = sqrDist;
                //            closestVertexIndex = c;
                //        }
                //    }

                //    if(closestVertexIndex != -1)
                //    {
                //        var tmp = verts[v + 1];
                //        verts[v + 1] = verts[closestVertexIndex];
                //        verts[closestVertexIndex] = tmp;
                //    } 
                //}

                float accumulatedDistance = offset;
                var vertCount = verts.Length;

                while (currentIndex < vertCount - 1)
                {
                    var actualIndex = reverse ? vertCount - currentIndex - 1 : currentIndex;
                    int nextAdd = reverse ? -1 : 1;
                    var v = verts[actualIndex];
                    var v2 = verts[actualIndex + nextAdd];
                    var n = normals[actualIndex];
                    var n2 = normals[actualIndex + nextAdd];

                    v = tr.TransformPoint(v);
                    v2 = tr.TransformPoint(v2);

                    Debug.DrawRay(v, Vector3.up * 10, Color.red, 20);
                    Debug.DrawRay(v2, Vector3.up * 10, Color.red, 20);

                    var distance = Vector3.Distance(v, v2);

                    while(accumulatedDistance < distance)
                    {
                        var alpha = accumulatedDistance / distance;
                        var position = Vector3.LerpUnclamped(v, v2, alpha);
                        var finalNormal = Vector3.Lerp(n, n2, alpha);
                        accumulatedDistance += fixedInterval;
                        instantiate(position, finalNormal, targetParentTransform);

                        Debug.DrawRay(position, Vector3.up * 10, Color.yellow, 20);
                        Debug.DrawRay(position, finalNormal, Color.cyan, 20);
                    }
                    accumulatedDistance -= distance;
                    currentIndex++;
                }
            }

            if(GUILayout.Button("Удалить объекты в объекте-контейнере"))
            {
                foreach(Transform child in targetParent.transform)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            GUILayout.Space(30);

            

            if (GUILayout.Button("Отобразить порядок вершин"))
            {
                var tr = targetMesh.transform;
                var mf = targetMesh.GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                var verts = mesh.vertices;

                Color startColor = Color.yellow;
                Color endColor = Color.blue;
                var totalCount = verts.Length;
                var normals = calculateNormals(verts, tr);

                for (int i = 0; i < totalCount; i++)
                {
                    var actualIndex = reverse ? totalCount - i - 1 : i;
                    Vector3 v = verts[actualIndex];
                    var position = tr.TransformPoint(v);
                    var finalColor = Color.Lerp(startColor, endColor, actualIndex / (float)totalCount);
                    Debug.DrawRay(position, Vector3.up, finalColor, 10);
                    Debug.DrawRay(position, normals[actualIndex], Color.cyan, 10);
                }
            }
        }

        void instantiate(Vector3 position, Vector3 normal, Transform targetParentTransform)
        {
            position += worldSpaceOffset;
            position += normal * normalOffset;
            var rotation = Quaternion.LookRotation(normal, Vector3.up) * additionalRotationQ;

            if (targetParentTransform != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(targetPrefab, targetParentTransform) as GameObject;
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            else
            {
                Instantiate(targetPrefab, position, rotation);
            }
        }
    }
}
