using System;
using System.Collections.Generic;
using System.IO;
using TzarGames.Poly2Tri;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace TzarGames.Editor.WaterMeshGenerator
{
    public class WaterMeshGenerator : EditorWindow
    {
        [SerializeField] private WaterMeshGeneratorData data = new WaterMeshGeneratorData
        {
            Height = 100,
            Width = 100,
            FoamWidth = 2,
            HeightDivisions = 100,
            WidthDivisions = 100,
        };

        [SerializeField] private bool showGrid = true;

        [SerializeField]
        Vector3 center;

        [SerializeField]
        GeneratedWaterMeshInfo currentInfo;

        [SerializeField]
        GameObject currentGameObject;

        Vector3[] borderPoints = new Vector3[5];

        [MenuItem("Tzar Games/Утилиты/Генерация сетки для жидкостей")]
        static void open()
        {
            var window = GetWindow<WaterMeshGenerator>();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            showGrid = EditorGUILayout.Toggle("Показывать сетку", showGrid);

            var newData = new WaterMeshGeneratorData();

            Vector3 _center = center;

            if (currentInfo != null)
            {
                center = currentInfo.transform.position;
            }
            else
            {
                _center = EditorGUILayout.Vector3Field("Центр", center);
            }

            newData.Width = EditorGUILayout.FloatField("Ширина", data.Width);
            newData.Height = EditorGUILayout.FloatField("Высота", data.Height);

            newData.WidthDivisions = EditorGUILayout.IntField("Кол-во ячеек по ширине", data.WidthDivisions);
            newData.WidthDivisions = Mathf.Clamp(newData.WidthDivisions, 1, int.MaxValue);

            newData.HeightDivisions = EditorGUILayout.IntField("Кол-во ячеек по высоте", data.HeightDivisions);
            newData.HeightDivisions = Mathf.Clamp(newData.HeightDivisions, 1, int.MaxValue);

            newData.TraceLayers = LayerMaskField("Слои трассировки", data.TraceLayers);

            newData.FoamWidth = EditorGUILayout.FloatField("Длина волны", data.FoamWidth);
            newData.DepthTraceDistance = EditorGUILayout.FloatField("Расстояние трассировки глубины", data.DepthTraceDistance);

            newData.ColorChannelToWrite = EditorGUILayout.IntSlider("Канал цвета", data.ColorChannelToWrite, 0, 3);
            newData.DepthChannelToWrite = EditorGUILayout.IntSlider("Канал цвета глубины", data.DepthChannelToWrite, 0, 3);
            newData.InverseColor = EditorGUILayout.Toggle("Инверсия цвета", data.InverseColor);


            var _currentMeshObject = EditorGUILayout.ObjectField("Объект с сеткой", currentGameObject, typeof(GameObject), true) as GameObject;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "WaterMeshGenerator");
                data = newData;

                if (currentInfo != null)
                {
                    currentInfo.Data = data;
                    EditorUtility.SetDirty(currentInfo);
                }
                else
                {
                    center = _center;
                }

                if (_currentMeshObject != currentGameObject)
                {
                    currentGameObject = _currentMeshObject;
                    if (currentGameObject != null)
                    {
                        var info = currentGameObject.GetComponent<GeneratedWaterMeshInfo>();
                        if (info != null && info.Mesh != null)
                        {
                            currentInfo = info;
                            data = currentInfo.Data;
                        }
                        else
                        {
                            currentInfo = null;
                            currentGameObject = null;
                        }
                    }
                }
            }

            GUILayout.Space(20);
            
            if (GUILayout.Button("1 шаг - Сгенерировать объект с сеткой"))
            {
                generate();
            }

            if (currentInfo != null)
            {
                GUILayout.Space(10);
                
                if (GUILayout.Button("2 шаг - Трассировка"))
                {
                    traceMeshJob(currentInfo.Mesh, data, currentGameObject.transform);
                }

                GUILayout.Space(20);
                
                if (GUILayout.Button("3 шаг - Оптимизация сетки"))
                {
                    try
                    {
                        optimizeMeshJob(currentInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                
                GUILayout.Space(20);
                
                
                if (GUILayout.Button("Экспорт цвета вершин в текстуру"))
                {
                    exportToTexture();
                }
                
                // if (GUILayout.Button("Trace"))
                // {
                //     traceMesh(currentInfo.Mesh, data.FoamWidth, currentGameObject.transform, data.TraceLayers);
                // }
            }
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void generate()
        {
            var widthDivisions = data.WidthDivisions;
            var heightDivisions = data.HeightDivisions;
            var width = data.Width;
            var height = data.Height;

            var widthVertexCount = widthDivisions + 1;
            var heightVertexCount = heightDivisions + 1;

            var vertices = new Vector3[widthVertexCount * heightVertexCount];
            var normals = new Vector3[vertices.Length];
            var texCoords = new Vector2[vertices.Length];

            var widthInterval = width / widthDivisions;
            var heightInterval = height / heightDivisions;

            var startCorner = new Vector3(-width * 0.5f, 0, height * 0.5f);

            for (int y = 0; y < heightVertexCount; y++)
            {
                for (int x = 0; x < widthVertexCount; x++)
                {
                    var vIndex = x + y * widthVertexCount;
                    vertices[vIndex] = startCorner + new Vector3(x * widthInterval, 0, y * -heightInterval);

                    normals[vIndex] = Vector3.up;

                    texCoords[vIndex] = new Vector2((float)x / widthDivisions, (float)y / heightDivisions);
                }
            }

            // indicies
            var indexCount = widthDivisions * heightDivisions * 6;
            var indicies = new int[indexCount];

            for (int y = 0, index = 0; y < heightDivisions; y++)
            {
                for (int x = 0; x < widthDivisions; x++, index += 6)
                {
                    int i = x + y * (widthDivisions + 1);

                    indicies[index] = i;
                    indicies[index + 1] = i + 1;
                    indicies[index + 2] = i + 1 + widthDivisions;
                    indicies[index + 3] = i + 1 + widthDivisions;
                    indicies[index + 4] = i + 1;
                    indicies[index + 5] = i + widthDivisions + 2;
                }
            }

            if (currentInfo != null)
            {
                DestroyImmediate(currentInfo);
            }

            var mesh = new Mesh();

            mesh.indexFormat = GetIndexFormat(vertices.Length);

            mesh.vertices = vertices;
            mesh.SetIndices(indicies, MeshTopology.Triangles, 0);
            mesh.normals = normals;
            mesh.uv = texCoords;

            if (currentGameObject == null)
            {
                currentGameObject = new GameObject("Water mesh");
            }
            
            var filter = currentGameObject.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = currentGameObject.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = mesh;
            var renderer = currentGameObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                currentGameObject.AddComponent<MeshRenderer>();
            }

            var transform = currentGameObject.transform;
            transform.position = center;

            currentInfo = currentGameObject.GetComponent<GeneratedWaterMeshInfo>();
            if (currentInfo == null)
            {
                currentInfo = currentGameObject.AddComponent<GeneratedWaterMeshInfo>();
            }
            currentInfo.Mesh = mesh;
            currentInfo.Data = data;
        }

        private static IndexFormat GetIndexFormat(int vertCount)
        {
            if (vertCount > 65535)
            {
                return IndexFormat.UInt32;
            }
            else
            {
                return IndexFormat.UInt16;
            }
        }

        static bool checkMinDist(Collider c, Vector3 pos, Vector3 dir, float rayDist, ref float minDist)
        {
            RaycastHit hit;

            if (c.Raycast(new Ray(pos, dir), out hit, rayDist))
            {
                var dist = Vector3.Distance(pos, hit.point);

                //Debug.DrawLine(pos, hit.point, Color.magenta, 10);

                if (dist < minDist)
                {
                    minDist = dist;
                }
                return true;
            }
            return false;
        }

        static bool checkMinDistToPoint(Vector3 point, Collider c, Vector3 rayStart, Vector3 dir, float rayDist, ref float minDist)
        {
            RaycastHit hit;

            if (c.Raycast(new Ray(rayStart, dir), out hit, rayDist))
            {
                var dist = Vector3.Distance(point, hit.point);

                //Debug.DrawLine(pos, hit.point, Color.magenta, 10);

                if (dist < minDist)
                {
                    minDist = dist;
                }
                return true;
            }
            return false;
        }

        static void traceMeshJob(Mesh mesh, WaterMeshGeneratorData info, Transform transform)
        {
            var verts = mesh.vertices;
            var colors = new Color[verts.Length];

            const int raysPerVertex = 32;
            var rayDirs = new Vector3[raysPerVertex];
            var angleStep = 360.0f / raysPerVertex;
            float currentAngle = 0;
            float foamWidthInv = 1.0f / info.FoamWidth;
            float outsideCastDist = info.FoamWidth;

            //float foamWidthSq = Mathf.Sqrt((foamWidth * foamWidth) * 2.0f);
            
            for (int i = 0; i < raysPerVertex; i++)
            {
                currentAngle += angleStep;
                rayDirs[i] = Quaternion.AngleAxis(currentAngle, Vector3.up) * Vector3.forward;
                //Debug.DrawRay(Vector3.zero, rayDirs[i] * 10, Color.red, 30);
            }
            
            // check terrains
            var terrains = FindObjectsOfType<TerrainCollider>();
            var indicesToCheck = new List<int>();

            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];
                var rayStart = transform.TransformPoint(v);
                bool ignore = false;

                if (terrains != null)
                {
                    var terrainRay = new Ray(rayStart + Vector3.up * 10000.0f, Vector3.down);

                    foreach (var collider in terrains)
                    {
                        if (collider.Raycast(terrainRay, out var terrainHit, 20000))
                        {
                            if (terrainHit.point.y > rayStart.y)
                            {
                                ignore = true;
                                break;
                            }
                        }
                    }
                }

                if (ignore == false)
                {
                    indicesToCheck.Add(i);
                }
                else
                {
                    colors[i] = new Color();
                    
                    var color = Color.white;
                    color[info.ColorChannelToWrite] = info.InverseColor ? 0 : 1;
                    color[info.DepthChannelToWrite] = 0;
                    colors[i] = color;
                }
            }

            var raycastCommands =
                new NativeArray<RaycastCommand>(raysPerVertex * indicesToCheck.Count, Allocator.TempJob);
            
            var hits = new NativeArray<RaycastHit>(raysPerVertex * indicesToCheck.Count, Allocator.TempJob);
            
            var outsideRaycastCommands =
                new NativeArray<RaycastCommand>(indicesToCheck.Count * raysPerVertex, Allocator.TempJob);
            
            var outsideRaycastHits =
                new NativeArray<RaycastHit>(indicesToCheck.Count * raysPerVertex, Allocator.TempJob);
            
            var depthRaycastCommands =
                new NativeArray<RaycastCommand>(indicesToCheck.Count, Allocator.TempJob);
            
            var depthHits = new NativeArray<RaycastHit>(indicesToCheck.Count, Allocator.TempJob);
            
            try
            {
                var foamWidth = info.FoamWidth;
                var queryParams = new QueryParameters
                {
                    hitBackfaces = true,
                    layerMask = info.TraceLayers,
                    hitTriggers = QueryTriggerInteraction.Ignore,
                    hitMultipleFaces = true
                };
                
                for (int i = 0; i < indicesToCheck.Count; i++)
                {
                    var vertexIndex = indicesToCheck[i];
                    var v = verts[vertexIndex];
                    var rayStart = transform.TransformPoint(v);
                    
                    for (var index = 0; index < rayDirs.Length; index++)
                    {
                        var rayDir = rayDirs[index];
                        var command = new RaycastCommand(rayStart, rayDir, queryParams, foamWidth);
                        raycastCommands[i * raysPerVertex + index] = command;
                    }

                    // outside casts
                    for (var index = 0; index < rayDirs.Length; index++)
                    {
                        var rayDir = rayDirs[index];
                        var command = new RaycastCommand(rayStart - rayDir * outsideCastDist, rayDir, queryParams, outsideCastDist);
                        outsideRaycastCommands[i * raysPerVertex + index] = command;
                    }
                    
                    // depth casts
                    depthRaycastCommands[i] =
                        new RaycastCommand(rayStart, Vector3.down, queryParams, info.DepthTraceDistance);
                }

                var handle = RaycastCommand.ScheduleBatch(raycastCommands, hits, 64);
                var handle2 = RaycastCommand.ScheduleBatch(outsideRaycastCommands, outsideRaycastHits, 64);
                var handle3 = RaycastCommand.ScheduleBatch(depthRaycastCommands, depthHits, 64);
                JobHandle.CombineDependencies(handle, handle2, handle3).Complete();
                
                for (int i = 0; i < indicesToCheck.Count; i++)
                {
                    var minDist = info.FoamWidth;
                    int hitCount = 0;
                    
                    for (var index = 0; index < raysPerVertex; index++)
                    {
                        var hit = hits[i * raysPerVertex + index];
                    
                        if (hit.collider == false)
                        {
                            continue;
                        }
                        hitCount++;
                    
                        if (hit.distance < minDist)
                        {
                            minDist = hit.distance;
                        }
                    }

                    if (hitCount > 0)
                    {
                        minDist *= foamWidthInv;
                        minDist = Mathf.Clamp01(minDist);
                        minDist = 1.0f - minDist;    
                    }
                    else
                    {
                        bool hasHit = false;
                        
                        for (int j = 0; j < raysPerVertex; j++)
                        {
                            var hit = outsideRaycastHits[i * raysPerVertex + j];
                            if (hit.collider != null)
                            {
                                hasHit = true;
                                break;
                            }
                        }
                    
                        minDist = hasHit ? 1.0f : 0.0f;
                    
                        // if (hasHit)
                        // {
                        //     Debug.DrawRay(verts[i], Vector3.up, Color.blue, 30);
                        // }
                    }
                    
                    var depthHit = depthHits[i];
                    var depth = 1.0f;
                    
                    if (depthHit.collider)
                    {
                        depth = depthHit.distance / info.DepthTraceDistance;
                    }

                    var color = Color.white;
                    color[info.ColorChannelToWrite] = info.InverseColor ? 1.0f - minDist : minDist;
                    color[info.DepthChannelToWrite] = depth;
                    var vi = indicesToCheck[i];
                    colors[vi] = color;
                }
                
                using (var colorArray = new NativeArray<Color>(colors, Allocator.TempJob))
                {
                    int meshWidth = info.WidthDivisions + 1;
                    int meshHeight = info.HeightDivisions + 1;

                    var fixHolesJob = new FixHolesJob
                    {
                        MeshWidth = meshWidth,
                        MeshHeight = meshHeight,
                        Colors = colorArray,
                        ColorChannel = info.ColorChannelToWrite
                    };
                    fixHolesJob.Schedule().Complete();
                    
                    mesh.colors = colorArray.ToArray();
                }
            }
            finally
            {
                outsideRaycastCommands.Dispose();
                outsideRaycastHits.Dispose();
                raycastCommands.Dispose();
                hits.Dispose();
            }
        }

        static void optimizeMeshJob(GeneratedWaterMeshInfo info)
        {
            var mesh = info.Mesh;
            var colors = mesh.colors;
            
            int chunkSize = 64;

            int meshWidth = info.Data.WidthDivisions + 1;
            int meshHeight = info.Data.HeightDivisions + 1;
            int horizontalChunkCount = RemoveSimilarColorsJob.CalculateChunkDimensionSize(meshWidth, chunkSize);
            int verticalChunkCount = RemoveSimilarColorsJob.CalculateChunkDimensionSize(meshHeight, chunkSize);
            int totalChunkCount = horizontalChunkCount * verticalChunkCount;
            
            using (var colorArray = new NativeArray<Color>(colors, Allocator.Persistent))
            using (var removedIndexMap = new NativeParallelMultiHashMap<int, int>(totalChunkCount * ((chunkSize+1) * (chunkSize+1)), Allocator.Persistent))
            using(var removedIndexList = new NativeList<int>(Allocator.TempJob))
            {
                var job = new RemoveSimilarColorsJob
                {
                    ChunkSize = chunkSize,
                    MeshWidth = meshWidth,
                    MeshHeight = meshHeight,
                    Colors = colorArray,
                    RemovedIndexMap = removedIndexMap.AsParallelWriter()
                };
                
                var handle = job.Schedule(totalChunkCount, 128);
                handle.Complete();
                
                
                var buildListJob = new BuildRemovedListJob
                {
                    RemovedMap = removedIndexMap,
                    Result = removedIndexList
                };
                buildListJob.Run();
                
                var vertices = mesh.vertices;
                var uvs = mesh.uv;
                var normals = mesh.normals;
                
                
                // foreach (var i in removedIndexList)
                // {
                //     Debug.DrawRay(vertices[i], Vector3.up, Color.red, 30);
                // }

                var pointList = new List<TriangulationPoint>();
                var vertList = new List<Vector3>();
                var uvList = new List<Vector2>();
                var colorList = new List<Color>();
                var normalList = new List<Vector3>();

                PolygonPoint corner_bottom_left = null;
                PolygonPoint corner_bottom_right = null;
                PolygonPoint corner_top_left = null;
                PolygonPoint corner_top_right = null;

                var vertCount = vertices.Length;

                for (var i = 0; i < vertCount; i++)
                {
                    if (removedIndexList.Contains(i))
                    {
                        continue;
                    }
                    var vertex = vertices[i];

                    var point = new PolygonPoint(vertex.x, vertex.z, pointList.Count);
                    pointList.Add(point);

                    if (i == 0)
                    {
                        corner_bottom_left = point;
                    }
                    else if(i == meshWidth-1)
                    {
                        corner_bottom_right = point;
                    }
                    else if(i == vertCount-1)
                    {
                        corner_top_right = point;
                    }
                    else if(i == vertCount-meshWidth)
                    {
                        corner_top_left = point;
                    }
                    
                    vertList.Add(vertex);
                    colorList.Add(colors[i]);
                    uvList.Add(uvs[i]);
                    normalList.Add(normals[i]);
                }

                new DTSweepConstraint(corner_top_right, corner_top_left);
                new DTSweepConstraint(corner_top_right, corner_bottom_right);
                
                new DTSweepConstraint(corner_bottom_left, corner_top_left);
                new DTSweepConstraint(corner_bottom_left, corner_bottom_right);
                
                Debug.DrawLine(new Vector3((float)corner_top_right.X, 0, (float)corner_top_right.Y),
                    new Vector3((float)corner_top_left.X, 0, (float)corner_top_left.Y), Color.green, 30);
                
                Debug.DrawLine(new Vector3((float)corner_top_right.X, 0, (float)corner_top_right.Y),
                    new Vector3((float)corner_bottom_right.X, 0, (float)corner_bottom_right.Y), Color.green, 30);
                
                Debug.DrawLine(new Vector3((float)corner_bottom_left.X, 0, (float)corner_bottom_left.Y),
                    new Vector3((float)corner_top_left.X, 0, (float)corner_top_left.Y), Color.cyan, 30);
                
                Debug.DrawLine(new Vector3((float)corner_bottom_left.X, 0, (float)corner_bottom_left.Y),
                    new Vector3((float)corner_bottom_right.X, 0, (float)corner_bottom_right.Y), Color.cyan, 30);
                

                var pointSet = new PointSet(pointList);

                P2T.Triangulate(pointSet);
                
                mesh.SetIndices(new ushort[3], MeshTopology.Triangles, 0);
                mesh.indexFormat = GetIndexFormat(vertices.Length);
                mesh.vertices = vertList.ToArray();
                mesh.normals = normalList.ToArray();
                mesh.colors = colorList.ToArray();
                mesh.uv = uvList.ToArray();

                var newIndicies = new ushort[pointSet.Triangles.Count * 3];
                int indexCounter = 0;
                foreach (var triangle in pointSet.Triangles)
                {
                    for (int i = 2; i >= 0; i--)
                    {
                        var point = triangle.Points[i];
                        newIndicies[indexCounter] = (ushort)point.Index;
                        indexCounter++;
                    }
                }
                mesh.SetIndices(newIndicies, MeshTopology.Triangles, 0);
                
                EditorUtility.SetDirty(mesh);
            }
        }

        [BurstCompile]
        struct BuildRemovedListJob : IJob
        {
            public int MeshWidth;
            public int MeshHeight;
            public NativeParallelMultiHashMap<int, int> RemovedMap;
            public NativeList<int> Result;
            
            public void Execute()
            {
                foreach (var keyValue in RemovedMap)
                {
                    if (Result.Contains(keyValue.Value))
                    {
                        continue;
                    }
                    Result.Add(keyValue.Value);
                }
            }
        }

        [BurstCompile]
        struct FixHolesJob : IJob
        {
            public int MeshWidth;
            public int MeshHeight;
            public int ColorChannel;

            public NativeArray<Color> Colors;

            public void Execute()
            {
                for (var index = 0; index < Colors.Length; index++)
                {
                    ExecuteForColorIndex(index);
                }
            }

            public void ExecuteForColorIndex(int colorIndex)
            {
                var targetColor = Colors[colorIndex];

                var c = targetColor[ColorChannel];

                var x = colorIndex % MeshWidth;
                var y = colorIndex / MeshWidth;

                var isNotWhite = new bool4(false);
                float min = float.MaxValue;
                float avg = 0;
                int avgCount = 0;
                
                // 0 - left
                // 1 - right
                // 2 - top
                // 3 - bottom

                if (x > 0)
                {
                    var otherIndex = x - 1 + MeshWidth * y;
                    var colorOnLeft = Colors[otherIndex][ColorChannel];

                    if (colorOnLeft < c)
                    {
                        if (min > colorOnLeft)
                        {
                            min = colorOnLeft;
                        }
                        isNotWhite[0] = true;
                    }
                    avg += colorOnLeft;
                    avgCount++;
                }
                else
                {
                    isNotWhite[0] = true;
                }

                if (x < MeshWidth-1)
                {
                    var otherIndex = x + 1 + MeshWidth * y;
                    var colorOnRight = Colors[otherIndex][ColorChannel];
                    
                    if (colorOnRight < c)
                    {
                        if (min > colorOnRight)
                        {
                            min = colorOnRight;
                        }
                        isNotWhite[1] = true;
                    }
                    avg += colorOnRight;
                    avgCount++;
                }
                else
                {
                    isNotWhite[1] = true;
                }

                if (y > 0)
                {
                    var otherIndex = x + MeshWidth * (y - 1);
                    var colorOnTop = Colors[otherIndex][ColorChannel];

                    if (colorOnTop < c)
                    {
                        if (min > colorOnTop)
                        {
                            min = colorOnTop;
                        }
                        isNotWhite[2] = true;
                    }
                    avg += colorOnTop;
                    avgCount++;
                }
                else
                {
                    isNotWhite[2] = true;
                }

                if (y < MeshHeight-1)
                {
                    var otherIndex = x + MeshWidth * (y + 1);
                    var colorOnBottom = Colors[otherIndex][ColorChannel];

                    if (colorOnBottom < c)
                    {
                        if (min > colorOnBottom)
                        {
                            min = colorOnBottom;
                        }
                        isNotWhite[3] = true;
                    }
                    avg += colorOnBottom;
                    avgCount++;
                }
                else
                {
                    isNotWhite[3] = true;
                }

                if (isNotWhite.x && isNotWhite.y && isNotWhite.z && isNotWhite.w)
                {
                    //Debug.Log($"fixing color {colorIndex}");
                    if (avgCount == 0)
                    {
                        targetColor[ColorChannel] = min;    
                    }
                    else
                    {
                        targetColor[ColorChannel] = avg / avgCount;
                    }
                    
                    Colors[colorIndex] = targetColor;
                }
            }
        }

        [BurstCompile]
        struct RemoveSimilarColorsJob : IJobParallelFor
        {
            public int ChunkSize;
            public int MeshWidth;
            public int MeshHeight;
            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<Color> Colors;
            
            public NativeParallelMultiHashMap<int,int>.ParallelWriter RemovedIndexMap;
            
            public void Execute(int chunkIndex)
            {
                // определяем стартовые координаты ячейки по данному индексу
                int2 min, max;

                int horizChunkCount = CalculateChunkDimensionSize(MeshWidth, ChunkSize);

                int chunkX = chunkIndex % horizChunkCount;
                int chunkY = chunkIndex / horizChunkCount;

                int chunkStartIndex = chunkX * (ChunkSize - 1) 
                                      + chunkY * MeshWidth * (ChunkSize - 1);

                min.x = chunkStartIndex % MeshWidth;
                max.x = math.min(min.x + (ChunkSize-1), MeshWidth-1);

                min.y = chunkStartIndex / MeshWidth;
                max.y = math.min(min.y + (ChunkSize - 1), MeshHeight-1);
                
                // расширяем ограничения на 1 единицу для того чтобы краевые ячейки стали смежными между чанками
                if (min.x > 0) min.x--;
                if (min.y > 0) min.y--;

                if (max.x < MeshWidth - 1) max.x++;
                if (max.y < MeshHeight - 1) max.y++;

                //Debug.Log($"Processing {chunkIndex} chunk ({chunkX},{chunkY}) with min ({min.x},{min.y}) and max ({max.x},{max.y})");
                
                var indiciesToRemove = new NativeList<int>(Colors.Length, Allocator.Temp);
                
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int x = min.x; x <= max.x; x++)
                    {
                        var colorPointIndex = x + y * MeshWidth;
                        CheckColorPoint(colorPointIndex, min, max, ref indiciesToRemove);        
                    }
                }
                
                foreach (var index in indiciesToRemove)
                {
                    //Debug.Log($"{chunkIndex} Removed index {index}");
                    RemovedIndexMap.Add(chunkIndex, index);
                }
            }

            public static int CalculateChunkDimensionSize(int meshSize, int chunkSize)
            {
                int result = meshSize / chunkSize;
                
                if (meshSize % chunkSize > 0)
                {
                    result++;
                }

                return result;
            }
            
            private void CheckColorPoint(int colorPointIndex, int2 min, int2 max, ref NativeList<int> indiciesToRemove)
            {
                int x = colorPointIndex % MeshWidth;
                int y = colorPointIndex / MeshWidth;

                checkNeighbourColors(colorPointIndex, x, y, min, max, out int4 neighbourIndices);

                bool hasDifferentNeightbour = false;

                for (int i = 0; i < 4; i++)
                {
                    if (neighbourIndices[i] == -1)
                    {
                        hasDifferentNeightbour = true;
                        break;
                    }
                }

                if (hasDifferentNeightbour)
                {
                    return;
                }

                var indiciesToCheck = new NativeList<int>(ChunkSize * 2, Allocator.Temp);
                indiciesToCheck.Add(colorPointIndex);

                while (indiciesToCheck.Length > 0)
                {
                    var currentIndex = indiciesToCheck[indiciesToCheck.Length - 1];
                    indiciesToCheck.RemoveAt(indiciesToCheck.Length - 1);

                    int ny = currentIndex / MeshWidth;
                    int nx = currentIndex % MeshWidth;

                    checkNeighbourColors(currentIndex, nx, ny, min, max, out int4 neighbours);

                    hasDifferentNeightbour = false;
                    var edgeCounter = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        if (neighbours[i] == -1)
                        {
                            hasDifferentNeightbour = true;
                        }
                        else if (neighbours[i] == -2)
                        {
                            edgeCounter++;
                        }
                    }

                    if (hasDifferentNeightbour || edgeCounter > 1)
                    {
                        continue;
                    }

                    if (indiciesToRemove.Contains(currentIndex) == false)
                    {
                        indiciesToRemove.Add(currentIndex);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        var otherIndex = neighbours[i];

                        if (otherIndex == -2)
                        {
                            continue;
                        }

                        if (indiciesToRemove.Contains(otherIndex))
                        {
                            continue;
                        }

                        indiciesToCheck.Add(otherIndex);
                    }
                }
            }

            // 0 - left
            // 1 - right
            // 2 - top
            // 3 - bottom
            void checkNeighbourColors(int targetIndex, int x, int y, int2 min, int2 max, out int4 neighbourIndices)
            {   
                var targetColor = Colors[targetIndex];
                neighbourIndices = new int4(-2);
                
                if (x > min.x)
                {
                    var otherIndex = x - 1 + MeshWidth * y;
                    var colorOnLeft = Colors[otherIndex];
                    
                    neighbourIndices[0] = targetColor.Equals(colorOnLeft) ? otherIndex : -1;
                }

                if (x < max.x)
                {
                    var otherIndex = x + 1 + MeshWidth * y;
                    var colorOnRight = Colors[otherIndex];

                    neighbourIndices[1] = targetColor.Equals(colorOnRight) ? otherIndex : -1;
                }

                if (y > min.y)
                {
                    var otherIndex = x + MeshWidth * (y - 1);
                    var colorOnTop = Colors[otherIndex];

                    neighbourIndices[2] = targetColor.Equals(colorOnTop) ? otherIndex : -1;
                }
                
                if (y < max.y)
                {
                    var otherIndex = x + MeshWidth * (y + 1);
                    var colorOnBottom = Colors[otherIndex];

                    neighbourIndices[3] = targetColor.Equals(colorOnBottom) ? otherIndex : -1;
                }
            }
        }

        static void traceMesh(Mesh mesh, float foamWidth, Transform transform, LayerMask layers)
        {
            var verts = mesh.vertices;
            var colors = new Color[verts.Length];

            var halfExtents = new Vector3(foamWidth, 0, foamWidth);

            var dir1 = new Vector3(-1, 0, 0);
            var dir2 = new Vector3(-1, 0, 1);
            var dir3 = new Vector3(0, 0, 1);
            var dir4 = new Vector3(1, 0, 1);
            var dir5 = new Vector3(1, 0, 0);
            var dir6 = new Vector3(1, 0, -1);
            var dir7 = new Vector3(0, 0, -1);
            var dir8 = new Vector3(-1, 0, -1);

            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];

                var rayStart = transform.TransformPoint(v);

                //Debug.DrawLine(rayStart, rayStart + Vector3.up, Color.red, 10);

                var colliders = Physics.OverlapBox(rayStart, halfExtents, Quaternion.identity, layers);

                if (colliders != null && colliders.Length > 0)
                {
                    float minDist = foamWidth;

                    for (int c = 0; c < colliders.Length; c++)
                    {
                        var collider = colliders[c];

                        if (collider is TerrainCollider || collider is MeshCollider)
                        {
                            var r1 = checkMinDist(collider, rayStart, dir1, foamWidth, ref minDist);
                            var r2 = checkMinDist(collider, rayStart, dir2, foamWidth, ref minDist);
                            var r3 = checkMinDist(collider, rayStart, dir3, foamWidth, ref minDist);
                            var r4 = checkMinDist(collider, rayStart, dir4, foamWidth, ref minDist);
                            var r5 = checkMinDist(collider, rayStart, dir5, foamWidth, ref minDist);
                            var r6 = checkMinDist(collider, rayStart, dir6, foamWidth, ref minDist);
                            var r7 = checkMinDist(collider, rayStart, dir7, foamWidth, ref minDist);
                            var r8 = checkMinDist(collider, rayStart, dir8, foamWidth, ref minDist);

                            var mm = minDist;

                            checkMinDistToPoint(rayStart, collider, rayStart + dir2 * foamWidth, -dir2, foamWidth * 2, ref mm);
                            checkMinDistToPoint(rayStart, collider, rayStart + dir4 * foamWidth, -dir4, foamWidth * 2, ref mm);
                            checkMinDistToPoint(rayStart, collider, rayStart + dir6 * foamWidth, -dir6, foamWidth * 2, ref mm);
                            checkMinDistToPoint(rayStart, collider, rayStart + dir8 * foamWidth, -dir8, foamWidth * 2, ref mm);

                            if (mm < minDist)
                            {
                                minDist = 0;
                            }

                            //if (r1 == false 
                            //    && r2 == false 
                            //    && r3 == false 
                            //    && r4 == false 
                            //    && r5 == false 
                            //    && r6 == false 
                            //    && r7 == false
                            //    && r8 == false
                            //    )
                            //{
                            //    RaycastHit hit;

                            //    if (collider.Raycast(new Ray(rayStart + dir2 * foamWidth, -dir2), out hit, foamWidth)) minDist = 0;
                            //    else if (collider.Raycast(new Ray(rayStart + dir4 * foamWidth, -dir4), out hit, foamWidth)) minDist = 0;
                            //    else if (collider.Raycast(new Ray(rayStart + dir6 * foamWidth, -dir6), out hit, foamWidth)) minDist = 0;
                            //    else if (collider.Raycast(new Ray(rayStart + dir8 * foamWidth, -dir8), out hit, foamWidth)) minDist = 0;
                            //}
                        }
                        else
                        {
                            var closest = colliders[c].ClosestPoint(rayStart);
                            var dist = Vector3.Distance(rayStart, closest);

                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }
                    }

                    var normDistance = 1 - minDist / foamWidth;
                    colors[i] = new Color(normDistance, normDistance, normDistance);

                    //Debug.Log("point " + i + " dist " + minDist);
                }
                else
                {
                    colors[i] = Color.black;
                }
            }

            mesh.colors = colors;
        }

        void exportToTexture()
        {
            var path = EditorUtility.SaveFilePanelInProject("Сохранение", "water_mesh_mask", "png", "Выберите путь для сохранения текстуры");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            var colors = currentInfo.Mesh.colors;
            
            var xPixels = currentInfo.Data.WidthDivisions + 1;
            var yPixels = currentInfo.Data.HeightDivisions + 1;

            var tex = new Texture2D(xPixels, yPixels);
            tex.SetPixels(colors);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            
            File.WriteAllBytes(path, bytes);
            
            AssetDatabase.ImportAsset(path);
        }

        static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }
            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            var newPos = Handles.PositionHandle(center, Quaternion.identity);

            if (center != newPos)
            {
                center = newPos;
                Undo.RecordObject(this, "WaterMeshGenerator");
            }

            if (showGrid == false)
            {
                return;
            }

            var widthDivisions = data.WidthDivisions;
            var heightDivisions = data.HeightDivisions;
            var width = data.Width;
            var height = data.Height;

            var halfW = width * 0.5f;
            var halfH = height * 0.5f;


            borderPoints[0] = center + new Vector3(halfW, 0, halfH);
            borderPoints[1] = center + new Vector3(-halfW, 0, halfH);
            borderPoints[2] = center + new Vector3(-halfW, 0, -halfH);
            borderPoints[3] = center + new Vector3(halfW, 0, -halfH);
            borderPoints[4] = center + new Vector3(halfW, 0, halfH);

            Handles.color = Color.blue;
            Handles.DrawPolyLine(borderPoints);

            if (widthDivisions > 1)
            {
                float interval = height / widthDivisions;
                List<Vector3> lines = new List<Vector3>();

                var startCorner = center + new Vector3(-width * 0.5f, 0, -height * 0.5f);

                for (int i = 1; i < widthDivisions; i++)
                {
                    var lineStart = startCorner + new Vector3(0, 0, interval * i);
                    var lineEnd = startCorner + new Vector3(width, 0, interval * i);

                    lines.Add(lineStart);
                    lines.Add(lineEnd);
                }

                Handles.DrawLines(lines.ToArray());
            }

            if (heightDivisions > 1)
            {
                float interval = width / heightDivisions;
                List<Vector3> lines = new List<Vector3>();

                var startCorner = center + new Vector3(-width * 0.5f, 0, -height * 0.5f);

                for (int i = 1; i < heightDivisions; i++)
                {
                    var lineStart = startCorner + new Vector3(interval * i, 0, 0);
                    var lineEnd = startCorner + new Vector3(interval * i, 0, height);

                    lines.Add(lineStart);
                    lines.Add(lineEnd);
                }

                Handles.DrawLines(lines.ToArray());
            }

            //Handles.color = Color.yellow;
            //Handles.DrawLine(center, center + Vector3.up * data.RaycastHeight);
            //Handles.color = Color.red;
            //Handles.DrawLine(center, center + Vector3.down * data.RaycastDepth);

            sceneView.Repaint();

            //Handles.BeginGUI();

            //Handles.EndGUI();
        }
    }
}