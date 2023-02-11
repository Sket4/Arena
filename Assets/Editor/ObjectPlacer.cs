using Arena.Tools;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Arena.Editor
{
    public class ObjectPlacer : EditorWindow
    {
        public ObjectPlacerSettings Settings;

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

                normal.Normalize();

                normals[v] = normal;
                //Debug.DrawRay(tr.TransformPoint(verts[v]), normal * 3, Color.blue, 30);
            }
            return normals;
        }

        private void OnGUI()
        {
            Settings = EditorGUILayout.ObjectField("Настройки", Settings, typeof(ObjectPlacerSettings), true) as ObjectPlacerSettings;

            if(Settings == null)
            {
                GUILayout.Label($"Не указан объект с настройками. назначьте объект с добавленным {nameof(ObjectPlacerSettings)}");
                return;
            }

            if (GUILayout.Button("Расставить объекты по точкам исходного мешa"))
            {
                foreach(var setting in Settings.Settings)
                {
                    var tr = setting.TargetMesh.transform;
                    var mf = setting.TargetMesh;
                    var mesh = mf.sharedMesh;
                    var verts = mesh.vertices;
                    Transform targetParentTransform = setting.TargetParent != null ? setting.TargetParent.transform : null;
                    var normals = calculateNormals(verts, tr);

                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 v = verts[i];
                        var position = tr.TransformPoint(v);
                        instantiate(position, normals[i], targetParentTransform, setting);
                    }
                }
            }

            if (GUILayout.Button("Расставить объекты по точкам исходного мешa через фиксированный интервал"))
            {
                foreach (var setting in Settings.Settings)
                {
                    var tr = setting.TargetMesh.transform;
                    var mf = setting.TargetMesh;
                    var mesh = mf.sharedMesh;
                    var verts = mesh.vertices;
                    var normals = calculateNormals(verts, tr);
                    Transform targetParentTransform = setting.TargetParent != null ? setting.TargetParent.transform : null;


                    int currentIndex = 0;

                    float accumulatedDistance = setting.Offset;
                    var vertCount = verts.Length;
                    int instanceCount = 0;

                    while (currentIndex < vertCount - 1)
                    {
                        var actualIndex = setting.Reverse ? vertCount - currentIndex - 1 : currentIndex;
                        int nextAdd = setting.Reverse ? -1 : 1;
                        var v = verts[actualIndex];
                        var v2 = verts[actualIndex + nextAdd];
                        var n = normals[actualIndex];
                        var n2 = normals[actualIndex + nextAdd];

                        v = tr.TransformPoint(v);
                        v2 = tr.TransformPoint(v2);

                        Debug.DrawRay(v, Vector3.up * 10, Color.red, 20);
                        Debug.DrawRay(v2, Vector3.up * 10, Color.red, 20);

                        var distance = Vector3.Distance(v, v2);

                        while (accumulatedDistance < distance)
                        {
                            var alpha = accumulatedDistance / distance;
                            var position = Vector3.LerpUnclamped(v, v2, alpha);
                            var finalNormal = Vector3.Lerp(n, n2, alpha);
                            accumulatedDistance += setting.FixedInterval;
                            instantiate(position, finalNormal, targetParentTransform, setting);

                            Debug.DrawRay(position, Vector3.up * 10, Color.yellow, 20);
                            Debug.DrawRay(position, finalNormal, Color.cyan, 20);

                            instanceCount++;

                            if (setting.MaximumObjects > 0 && instanceCount >= setting.MaximumObjects)
                            {
                                break;
                            }
                        }

                        if (setting.MaximumObjects > 0 && instanceCount >= setting.MaximumObjects)
                        {
                            break;
                        }

                        accumulatedDistance -= distance;
                        currentIndex++;
                    }
                }
            }

            if(GUILayout.Button("Удалить объекты в объекте-контейнере"))
            {
                var childs = new List<Transform>();

                foreach (var setting in Settings.Settings)
                {
                    foreach (Transform child in setting.TargetParent.transform)
                    {
                        childs.Add(child);
                    }
                }

                foreach(var child in childs)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            GUILayout.Space(30);

            

            if (GUILayout.Button("Отобразить порядок вершин"))
            {
                foreach (var setting in Settings.Settings)
                {
                    var tr = setting.TargetMesh.transform;
                    var mf = setting.TargetMesh;
                    var mesh = mf.sharedMesh;
                    var verts = mesh.vertices;

                    Color startColor = Color.yellow;
                    Color endColor = Color.blue;
                    var totalCount = verts.Length;
                    var normals = calculateNormals(verts, tr);

                    for (int i = 0; i < totalCount; i++)
                    {
                        var actualIndex = setting.Reverse ? totalCount - i - 1 : i;
                        Vector3 v = verts[actualIndex];
                        var position = tr.TransformPoint(v);
                        var finalColor = Color.Lerp(startColor, endColor, actualIndex / (float)totalCount);
                        Debug.DrawRay(position, Vector3.up, finalColor, 10);
                        Debug.DrawRay(position, normals[actualIndex], Color.cyan, 10);
                    }
                }
            }
        }

        void instantiate(Vector3 position, Vector3 normal, Transform targetParentTransform, PlacerSettings placerSettings)
        {
            position += placerSettings.WorldSpaceOffset;
            position += normal * placerSettings.NormalOffset;
            var rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(placerSettings.AdditionalRotation);

            if (targetParentTransform != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(placerSettings.TargetPrefab, targetParentTransform) as GameObject;
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            else
            {
                Instantiate(placerSettings.TargetPrefab, position, rotation);
            }
        }
    }
}
