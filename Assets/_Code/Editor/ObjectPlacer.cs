using System;
using Arena.Tools;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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

        Vector3[] calculateNormals(Vector3[] verts)
        {
            Vector3[] normals = new Vector3[verts.Length];

            for (int v = 0; v < verts.Length; v++)
            {
                Vector3 normal;

                if (v == 0)
                {
                    var vertex = verts[0];
                    var nextVertex = verts[1];
                    var dir = (nextVertex - vertex).normalized;
                    normal = Vector3.Cross(Vector3.up, dir);
                }
                else if (v == verts.Length - 1)
                {
                    var nextVertex = verts[verts.Length - 1];
                    var vertex = verts[verts.Length - 2];
                    var dir = (nextVertex - vertex).normalized;
                    normal = Vector3.Cross(Vector3.up, dir);
                }
                else
                {
                    var prevVertex = verts[v - 1];
                    var vertex = verts[v];
                    var nextVertex = verts[v + 1];


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

        static Vector3[] GetVertices(PlacerSettings settings)
        {
            var path = AssetDatabase.GetAssetPath(settings.PathObject);

            if (path.EndsWith("obj") == false)
            {
                EditorUtility.DisplayDialog(title: "Неправильный файл", message: "Назначен неверный меш для построения пути. Поддерживается только OBJ формат", ok: "OK");
                throw new Exception("поддерживаются только OBJ файлы");
            }
            
            var lines = File.ReadAllLines(path);
            var vertices = new List<Vector3>(lines.Length);
            var transformMatrix = Matrix4x4.Scale(settings.PathObjectScale);
            
            foreach (var line in lines)
            {
                if (line.StartsWith("v ") == false)
                {
                    continue;
                }
                var parsed = line.Replace("v ", "");
                var splitted = parsed.Split();

                if (splitted.Length != 3)
                {
                    Debug.LogError("Неверное количество чисел для постороения вершины");
                    continue;
                }

                var vert = new Vector3();
                vert.x = float.Parse(splitted[0].Trim().Replace('.', ','));
                vert.y = float.Parse(splitted[1].Trim().Replace('.', ','));
                vert.z = float.Parse(splitted[2].Trim().Replace('.', ','));

                vert = transformMatrix * vert;
                
                vertices.Add(vert);
            }
            return vertices.ToArray();
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
                    var verts = GetVertices(setting);
                    Transform targetParentTransform = setting.TargetParent != null ? setting.TargetParent.transform : null;
                    var normals = calculateNormals(verts);

                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 v = verts[i];
                        instantiate(v, normals[i], targetParentTransform, setting);
                    }
                }
            }

            if (GUILayout.Button("Расставить объекты по точкам исходного мешa через фиксированный интервал"))
            {
                foreach (var setting in Settings.Settings)
                {
                    var verts = GetVertices(setting);
                    var normals = calculateNormals(verts);
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
                    var verts = GetVertices(setting);

                    Color startColor = Color.yellow;
                    Color endColor = Color.blue;
                    var totalCount = verts.Length;
                    var normals = calculateNormals(verts);

                    for (int i = 0; i < totalCount; i++)
                    {
                        var actualIndex = setting.Reverse ? totalCount - i - 1 : i;
                        Vector3 v = verts[actualIndex];
                        var finalColor = Color.Lerp(startColor, endColor, actualIndex / (float)totalCount);
                        Debug.DrawRay(v, Vector3.up, finalColor, 10);
                        Debug.DrawRay(v, normals[actualIndex], Color.cyan, 10);
                    }
                }
            }
        }

        void instantiate(Vector3 position, Vector3 normal, Transform targetParentTransform, PlacerSettings placerSettings)
        {
            position += placerSettings.WorldSpaceOffset;
            position += normal * placerSettings.NormalOffset;
            var rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(placerSettings.AdditionalRotation);

            if (placerSettings.RandomYaw)
            {
                rotation *= Quaternion.Euler(0,Random.Range(0, 360),0);
            }

            GameObject instance;
            
            if (targetParentTransform != null)
            {
                instance = PrefabUtility.InstantiatePrefab(placerSettings.TargetPrefab, targetParentTransform) as GameObject;
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            else
            {
                instance = Instantiate(placerSettings.TargetPrefab, position, rotation);
            }
            
            if (placerSettings.ChangeScale)
            {
                var randomScale = Vector3.one;
                randomScale.x = Random.Range(placerSettings.MinScale.x, placerSettings.MaxScale.x);
                randomScale.y = Random.Range(placerSettings.MinScale.y, placerSettings.MaxScale.y);
                randomScale.z = Random.Range(placerSettings.MinScale.z, placerSettings.MaxScale.z);
                
                instance.transform.localScale = randomScale;
            }
        }
    }
}
