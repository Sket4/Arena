using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Arena.Maze
{
    public enum BuildMazeRequestState : byte
    {
        Pending,
        Building,
        Completed
    }

    public struct BuildMazeRequest : IComponentData
    {
        public uint Seed;
        public BuildMazeRequestState State;
        public Entity Builder;
        public Entity MazeEntity;
        public int HorizontalCells;
        public int VerticalCells;
        public int StartCellCount;
    }

    public struct DestroyMazeRequest : IComponentData
    {
        public Entity MazeEntity;
    }

    public struct MazeObjects : ICleanupBufferElementData
    {
        public Entity Value;
    }

    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class MazeBuilderSystem : GameSystemBase
    {
        EntityArchetype cellArchetype;

        protected override void OnCreate()
        {
            base.OnCreate();
            cellArchetype = EntityManager.CreateArchetype(typeof(MazeCell), typeof(MazeCellNeighbors), typeof(MazeCellWalls), typeof(MaceCellRemovedWalls));
        }

        protected override void OnSystemUpdate()
        {
            var commands = CreateUniversalCommandBuffer();

            Entities
                .WithoutBurst()
                .ForEach((Entity requestEntity, int entityInQueryIndex, ref BuildMazeRequest buildRequest) =>
            {
                if(buildRequest.State == BuildMazeRequestState.Completed)
                {
                    return;
                }

                commands.DefaultSortKey = entityInQueryIndex;

                var random = new Random(buildRequest.Seed);

                if(buildRequest.State == BuildMazeRequestState.Pending)
                {
                    Debug.Log("Генерация основы лабиринта");
                    Entity mazeEntity;
                    if(buildRequest.MazeEntity == requestEntity)
                    {
                        mazeEntity = requestEntity;
                    }
                    else
                    {
                        mazeEntity = commands.CreateEntityWithDefaultKey();
                        buildRequest.MazeEntity = mazeEntity;
                    }

                    buildRequest.State = BuildMazeRequestState.Building;
                    commands.SetComponentWithDefaultKey(requestEntity, buildRequest);

                    var builder = SystemAPI.GetComponent<MazeWorldBuilder>(buildRequest.Builder);

                    GenerateMaze(mazeEntity, buildRequest.Builder, buildRequest.Seed, buildRequest.HorizontalCells, buildRequest.VerticalCells, buildRequest.StartCellCount, ref random, ref commands);
                    return;
                }

                Debug.Log("Построение геометрии лабиринта");
                ProcessMaze(buildRequest.MazeEntity, buildRequest.Builder, ref random, ref commands);
                buildRequest.State = BuildMazeRequestState.Completed;

            }).Run();

            Entities
                .WithoutBurst()
                .WithNone<Maze>()
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<MazeCells> mazeCells) =>
            {
                foreach(var cell in mazeCells)
                {
                    commands.DestroyEntity(entityInQueryIndex, cell.Cell);
                }
                commands.RemoveComponent<MazeCells>(entityInQueryIndex, entity);

            }).Run();

            Entities
                .WithoutBurst()
                .WithNone<Maze>()
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<MazeObjects> mazeObjs) =>
                {
                    foreach (var cell in mazeObjs)
                    {
                        commands.DestroyEntity(entityInQueryIndex, cell.Value);
                    }
                    commands.RemoveComponent<MazeObjects>(entityInQueryIndex, entity);

                }).Run();

            Entities
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex, in DestroyMazeRequest request) =>
            {
                commands.DestroyEntity(entityInQueryIndex, request.MazeEntity);
                commands.DestroyEntity(entityInQueryIndex, entity);

            }).Run();
        }

        // находит длиннейший путь в лабиринте. Если путь не найден - возвращается false
        public bool FindLongestPath(in Maze maze, Entity mazeEntity, int firstCell, int secondCell, List<int> Path)
        {
            Satsuma.CustomGraph graph = new Satsuma.CustomGraph();

            int i, j, temp;
            var nodesToCells = new Dictionary<long, int>();
            var cellToNodes = new Dictionary<int, Satsuma.Node>();

            // заполняем массив графов в соответствии с ячейками лабиринта
            var mazeCells = EntityManager.GetBuffer<MazeCells>(mazeEntity);

            for (i = 0; i < mazeCells.Length; i++)
            {
                var node = graph.AddNode();
                nodesToCells.Add(node.Id, i);

                if (cellToNodes.ContainsKey(i) == false)
                {
                    cellToNodes.Add(i, node);
                }

                var cellEntity = mazeCells[i].Cell;
                var removedWalls = EntityManager.GetBuffer<MaceCellRemovedWalls>(cellEntity);

                for (j = 0; j < removedWalls.Length; j++)
                {
                    temp = removedWalls[j].CellIndex;

                    if (cellToNodes.ContainsKey(temp) == false)
                    {
                        var tmpNode = graph.AddNode();

                        if (nodesToCells.ContainsKey(tmpNode.Id) == false)
                        {
                            nodesToCells.Add(tmpNode.Id, temp);
                        }

                        if (cellToNodes.ContainsKey(temp) == false)
                        {
                            cellToNodes.Add(temp, tmpNode);
                        }
                    }
                    graph.AddArc(cellToNodes[i], cellToNodes[temp], Satsuma.Directedness.Undirected);

                    //graph.Vertexes[i].LinkedVertexes.AddItem(temp);
                    //graph.Vertexes[temp].LinkedVertexes.AddItem(i);
                }
            }

            //class'TG_GraphLibrary'.static.FindLongestPath(graph, firstCell, secondCell, gpath);
            var dijkstra = new Satsuma.Dijkstra(graph, arc =>
            {
                var source = graph.U(arc);
                var target = graph.V(arc);

                var sourceCellId = nodesToCells[source.Id];
                var targetCellId = nodesToCells[target.Id];

                var sourcePos = mazeCells[sourceCellId].Position;
                var targetPos = mazeCells[targetCellId].Position;

                return math.distancesq(targetPos, sourcePos);

            }, Satsuma.DijkstraMode.Sum);

            var startNode = cellToNodes[firstCell];
            var endNode = cellToNodes[secondCell];

            dijkstra.AddSource(startNode);
            dijkstra.RunUntilFixed(endNode);

            var path = dijkstra.GetPath(endNode);
            if (path == null)
            {
                return false;
            }

            foreach (var n in path.Nodes())
            {
                Path.Add(nodesToCells[n.Id]);
            }

            return true;
        }

        void ProcessMaze(Entity mazeEntity, Entity mazeBuilderEntity, ref Random random, ref UniversalCommandBuffer commands)
        {
            int maxVersionCount = 3;

            var calculatedPaths = new List<List<int>>();

            for (int i = 0; i < maxVersionCount; i++)
            {
                calculatedPaths.Add(new List<int>());
            }

            int maxPathLen = 0, maxPathNum = 0;

            var maze = GetComponent<Maze>(mazeEntity);

            var totalCellCount = maze.HorizontalCells * maze.VerticalCells;
            FindLongestPath(maze, mazeEntity, 0, totalCellCount - 1, calculatedPaths[0]);
            FindLongestPath(maze, mazeEntity, 0, totalCellCount - maze.HorizontalCells, calculatedPaths[1]);
            FindLongestPath(maze, mazeEntity, 0, maze.HorizontalCells - 1, calculatedPaths[2]);

            //FindLongestPath(maze, mazeEntity, maze.HorizontalCells - 1, totalCellCount - 1, calculatedPaths[3]);
            //FindLongestPath(maze, mazeEntity, maze.HorizontalCells - 1, totalCellCount - maze.HorizontalCells, calculatedPaths[4]);
            //FindLongestPath(maze, mazeEntity, totalCellCount - maze.HorizontalCells, totalCellCount - 1, calculatedPaths[5]);

            for (int i = 0; i < calculatedPaths.Count; i++)
            {
                if (maxPathLen < calculatedPaths[i].Count)
                {
                    maxPathLen = calculatedPaths[i].Count;
                    maxPathNum = i;
                }
            }

            Debug.Log("MaxPath:" + calculatedPaths[maxPathNum].Count);

            SetComponent(mazeEntity, maze);

            BuildMaze(mazeEntity, mazeBuilderEntity, ref random, ref commands, calculatedPaths[maxPathNum]);

            //var lightController = mazeContainer.gameObject.AddComponent<MazeLightController>();
            //lightController.TraceLayers = lightTracelayers;
            //lightController.Initialize(maxLightRadius, maxLightCount);

            //GenerateCoins(10, maze);

            //GenerateSpawners(maze);
            //GenerateChests(maze);

            //var navMesh = mazeContainer.gameObject.AddComponent<UnityEngine.AI.NavMeshSurface>();
            //navMesh.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            //navMesh.layerMask = navMeshLayers;
            //navMesh.BuildNavMesh();

            //var mapBoundsObj = new GameObject("Map bounds");
            //mapBoundsObj.transform.SetParent(mazeContainer);
            //var mapBounds = mapBoundsObj.AddComponent<MapBounds>();
            //var min = float3.zero;
            //var max = float3.zero;
            //min.x = Mathf.Min(MazeGlobalCorner1.x, MazeGlobalCorner2.x);
            //min.y = Mathf.Min(MazeGlobalCorner1.y, MazeGlobalCorner2.y);
            //min.z = Mathf.Min(MazeGlobalCorner1.z, MazeGlobalCorner2.z);
            //min.x = Mathf.Min(MazeGlobalCorner1.x, MazeGlobalCorner2.x);
            //max.y = Mathf.Max(MazeGlobalCorner1.y, MazeGlobalCorner2.y);
            //max.z = Mathf.Max(MazeGlobalCorner1.z, MazeGlobalCorner2.z);
            //mapBounds.CreateBoundsFromMinMax(min, max);

            //foreach(var m in mp)
            //{
            //    m.Dispose();
            //}
            //mp.Dispose();
        }

        static readonly quaternion rot180 = quaternion.AxisAngle(math.up(), math.radians(180));

        Entity spawnBorderCell(DynamicBuffer<BorderCellPrefabs> prefabs, float3 position, float angle, ref DynamicBuffer<MazeObjects> linkedEntities, ref Random random, ref UniversalCommandBuffer commands)
        {
            if(prefabs.IsEmpty)
            {
                return Entity.Null;
            }
            
            quaternion rot;

            rot = quaternion.Euler(0, math.radians(angle), 0);

            var randCell = random.NextInt(0, prefabs.Length);
            var cell = commands.InstantiateWithDefaultKey(prefabs[randCell].Prefab);
            var l2w = LocalTransform.FromPositionRotation(position, rot);
            commands.SetComponentWithDefaultKey(cell, l2w);
            linkedEntities.Add(new MazeObjects() { Value = cell });

            return cell;
        }

        Entity spawnWall(DynamicBuffer<WallPrefabs> prefabs, float3 position, bool horizontal, ref DynamicBuffer<MazeObjects> linkedEntities, ref Random random, ref UniversalCommandBuffer commands, bool randomRot = true)
        {
            quaternion rot;

            if (horizontal)
                rot = quaternion.Euler(0, math.radians(90), 0);
            else
            {
                rot = quaternion.identity;
            }

            if (randomRot && random.NextInt(0, 2) > 0)
            {
                rot = math.mul(rot, rot180);
            }

            var randWall = random.NextInt(0, prefabs.Length);
            var wallPrefab = prefabs[randWall].Prefab;

            if(wallPrefab == Entity.Null)
            {
                Debug.LogError("Null wall prefab");
                return Entity.Null;
            }

            var wall = commands.InstantiateWithDefaultKey(prefabs[randWall].Prefab);
            commands.SetComponentWithDefaultKey(wall, LocalTransform.FromPositionRotation(position, rot));
            linkedEntities.Add(new MazeObjects() { Value = wall });

            return wall;
        }

//        void processExitWall(Entity wall, int wallDir)
//        {
//            var tags = wall.GetComponentsInChildren<Common.TagBehaviour>();
//            Common.TagBehaviour arrow = null;

//            foreach (var t in tags)
//            {
//                if (t.HasTag(exitWallArrowTag.ToString()))
//                {
//                    arrow = t;
//                    break;
//                }
//            }

//            if (arrow != null)
//            {
//                if (wallDir == 0 || wallDir == 1)
//                {
//                    var rot = Quaternion.Euler(0, 180, 0);
//                    arrow.transform.rotation *= rot;
//                }

//#if UNITY_EDITOR
//                Debug.Log("Arrow dir " + wallDir);
//#endif
//            }
//        }

        struct StartCellInfo
        {
            public int Index;
            public int SideIndex;
        }

        struct ColumnPositionInfo : IEquatable<ColumnPositionInfo>
        {
            public float3 Position;
            public bool IsBorder;

            public bool Equals(ColumnPositionInfo other)
            {
                return Position.Equals(other.Position);
            }
        }

        static float3 calculateCellPosition(int cellIndex, int horizontallCells, float cellSize, in float3 mazeStartOffset, ref int zCount)
        {
            var result = new float3();

            int c = cellIndex % horizontallCells;

            result.x = mazeStartOffset.x + -cellSize * 0.5f + -(cellSize * c);

            if (cellIndex != 0 && cellIndex % horizontallCells == 0)
            {
                zCount++;
            }
            result.z = mazeStartOffset.z + cellSize * 0.5f + (cellSize * zCount);

            return result;
        }

        struct AddedWallInfo : IEquatable<AddedWallInfo>
        {
            public int Cell_1;
            public int Cell_2;

            public AddedWallInfo(int cell1, int cell2)
            {
                Cell_1 = cell1;
                Cell_2 = cell2;
            }

            public bool Equals(AddedWallInfo other)
            {
                return Cell_1 == other.Cell_1 && Cell_2 == other.Cell_2
                    || Cell_1 == other.Cell_2 && Cell_2 == other.Cell_1;
            }
        }

        void BuildMaze(Entity mazeEntity, Entity mazeBuilder, ref Random random, ref UniversalCommandBuffer commands, List<int> path)
        {
            var startHeightTranslation = new float3(0, 0, 0);
            float3 currentCellLocation = startHeightTranslation, wallSpawnLocation;
            var columnPositions = new NativeList<ColumnPositionInfo>(64, Allocator.Temp);

            var gameCellPrefabs = EntityManager.GetBuffer<GameCellPrefabs>(mazeBuilder);

            var builderData = EntityManager.GetComponentData<MazeWorldBuilder>(mazeBuilder);

            var cellSize = builderData.CellSize;
            var maze = EntityManager.GetComponentData<Maze>(mazeEntity);
            var mazeCells = GetBuffer<MazeCells>(mazeEntity);

            var lastMazeCellIndex = path[path.Count - 1];
            var lastMazeCell = GetComponent<MazeCell>(mazeCells[lastMazeCellIndex].Cell);
            maze.LastZone = lastMazeCell.ZoneId;
            EntityManager.SetComponentData(mazeEntity, maze);
            
            //float3 MazeGlobalCorner2 = default;

            // обрабатываем каждую ячейку лабиринта
            //var exitWallArray = new MazeBuildObject[] { ExitWall };
            //var enterWallArray = new MazeBuildObject[] { EnterWall };

            
            var wallPrefabs = GetBuffer<WallPrefabs>(mazeBuilder);
            var doorWallPrefabs = GetBuffer<DoorWallPrefabs>(mazeBuilder);
            var borderCellPrefabs = GetBuffer<BorderCellPrefabs>(mazeBuilder);
            var environmentCellprefabs = GetBuffer<EnvironmentCellPrefabs>(mazeBuilder);

            var startCellPrefabs = GetBuffer<StartCellPrefabs>(mazeBuilder);
            var finishCellPrefabs = GetBuffer<FinishCellPrefabs>(mazeBuilder);
            var borderWallPrefabs = GetBuffer<BorderWallPrefabs>(mazeBuilder);
            var columnPrefabs = GetBuffer<ColumnPrefabs>(mazeBuilder);
            var borderColumnPrefabs = GetBuffer<BorderColumnPrefabs>(mazeBuilder);

            // init start cells
            //List<StartCellInfo> startCellList = initStartCells(maze, ref mazeCells);
            int startCellIndex = 0;

            // build
            var linkedEntities = commands.AddBufferWithDefaultKey<MazeObjects>(mazeEntity);

            // список индексов ячеек, для которых уже были добавлены стены с дверьми
            var doorProcessedCells = new List<int>();

            // зоны, которые пролегают по пути
            var zonesInPath = new List<int>();

            for (int i = 0; i < mazeCells.Length; i++)
            {
                if(path.Contains(i) == false)
                {
                    continue;
                }

                var mazeCell = GetComponent<MazeCell>(mazeCells[i].Cell);

                if(zonesInPath.Contains(mazeCell.ZoneId) == false)
                {
                    zonesInPath.Add(mazeCell.ZoneId);
                }
            }

            foreach (var zoneId in zonesInPath)
            {
                var zoneInstance = commands.InstantiateWithDefaultKey(builderData.ZonePrefab);
                commands.SetComponentWithDefaultKey(zoneInstance, new ZoneId((ushort)zoneId));
            }
            
            var addedWalls = new List<AddedWallInfo>();

            for (int i = 0; i < mazeCells.Length; i++)
            {
                var currentCellElement = mazeCells[i];
                var currentCellEntity = currentCellElement.Cell;
                var currentCellInfo = GetComponent<MazeCell>(currentCellEntity);
                bool isEnvironmentCell = false;

                if (zonesInPath.Contains(currentCellInfo.ZoneId) == false)
                {
                    // ячейка не проходит по пути, поэтому спауним для нее особые префабы
                    isEnvironmentCell = true;
                }

                currentCellLocation = mazeCells[i].Position;

                var isStartCell = startCellIndex == i;
                var isLastCell = lastMazeCellIndex == i;

                var prefabToSpawn = Entity.Null;

                if(isEnvironmentCell)
                {
                    if(environmentCellprefabs.Length > 0)
                    {
                        prefabToSpawn = environmentCellprefabs[random.NextInt(0, environmentCellprefabs.Length)].Prefab;
                    }
                }
                else if (isStartCell && startCellPrefabs.Length > 0)
                {
                    prefabToSpawn = startCellPrefabs[random.NextInt(0, startCellPrefabs.Length)].Prefab;
                }
                else if(isLastCell && finishCellPrefabs.Length > 0)
                {
                    prefabToSpawn = finishCellPrefabs[random.NextInt(0, finishCellPrefabs.Length)].Prefab;
                }
                else
                {
                    prefabToSpawn = gameCellPrefabs[random.NextInt(0, gameCellPrefabs.Length)].Prefab;
                }

                if(prefabToSpawn == Entity.Null)
                {
                    continue;
                }

                quaternion cellRotation;
                
                if(isStartCell)
                {
                    var sci = path.IndexOf(startCellIndex);
                    var nextIndex = path[sci + 1];
                    var nextCell = mazeCells[nextIndex];
                    var dirToNext = nextCell.Position - currentCellLocation;
                    dirToNext.y = 0;
                    dirToNext = math.normalize(dirToNext);

                    cellRotation = quaternion.LookRotation(dirToNext, math.up());
                }
                else if(isLastCell)
                {
                    var sci = path.IndexOf(lastMazeCellIndex);
                    var prevIndex = path[sci - 1];
                    var prevCell = mazeCells[prevIndex];
                    var dirToPrev = prevCell.Position - currentCellLocation;
                    dirToPrev.y = 0;
                    dirToPrev = math.normalize(dirToPrev);

                    cellRotation = quaternion.LookRotation(dirToPrev, math.up());
                }
                else
                {
                    float angle = 0;

                    switch (random.NextInt(0, 4))
                    {
                        case 0:
                            angle = 0;
                            break;
                        case 1:
                            angle = 90;
                            break;
                        case 2:
                            angle = 180;
                            break;
                        case 3:
                            angle = 270;
                            break;
                    }

                    cellRotation = quaternion.Euler(0, math.radians(angle), 0);
                }

                var spawnedCellEntity = commands.InstantiateWithDefaultKey(prefabToSpawn);
                commands.SetComponentWithDefaultKey(spawnedCellEntity, LocalTransform.FromPositionRotation(currentCellLocation, cellRotation));
                linkedEntities.Add(new MazeObjects() { Value = spawnedCellEntity });

                if(isEnvironmentCell)
                {
                    continue;
                }

                // проверяем ячейку и вычисляем мировые координаты крайних углов лабиринта
                //if (i == mazeCells.Length - 1)
                //{
                //    MazeGlobalCorner2.x = currentCellLocation.x - cellSize * 0.5f;
                //    MazeGlobalCorner2.z = currentCellLocation.z + cellSize * 0.5f;
                //}

                //bool isFinishWallPlaced = false;
                //bool isStartWallPlaced = false;
                var currentCellWalls = GetBuffer<MazeCellWalls>(currentCellEntity);
                var currentCellRemovedWalls = GetBuffer<MaceCellRemovedWalls>(currentCellEntity);

                commands.SetComponentWithDefaultKey(spawnedCellEntity, new ZoneId { Value = currentCellInfo.ZoneId });

                float3 borderCellLocation;
                bool isVerticalBorderCell = false;
                bool isHorizontalBorderCell = false;

                // left up right down
                var isLeftRemoved = false;
                var leftWallIndex = -1;
                var isUpRemoved = false;
                var upWallIndex = -1;
                var isRightRemoved = false;
                var rightWallIndex = -1;
                var isDownRemoved = false;
                var downWallIndex = -1;

                // спауним стены
                for (int j = 0; j < currentCellRemovedWalls.Length; j++)
                {
                    var removedCellIndex = currentCellRemovedWalls[j].CellIndex;

                    // проверка удаленных стенок
                    // проверяем, левая ли это стенка
                    if (removedCellIndex == i - 1)
                    {
                        isLeftRemoved = true;
                        leftWallIndex = removedCellIndex;
                    }

                    // проверяем, нижняя ли это стенка
                    if (removedCellIndex == i + maze.HorizontalCells)
                    {
                        isDownRemoved = true;
                        downWallIndex = removedCellIndex;
                    }

                    // проверяем, правая ли это стенка
                    if (removedCellIndex == i + 1)
                    {
                        isRightRemoved = true;
                        rightWallIndex = removedCellIndex;
                    }

                    // проверяем, нижняя ли это стенка
                    if (removedCellIndex == i - maze.HorizontalCells)
                    {
                        isUpRemoved = true;
                        upWallIndex = removedCellIndex;
                    }
                }

                for (int j = 0; j < currentCellWalls.Length; j++)
                {
                    var cellIndex = currentCellWalls[j].CellIndex;

                    // проверяем, левая ли это стенка
                    if (cellIndex == i - 1)
                    {
                        leftWallIndex = cellIndex;
                    }

                    // проверяем, нижняя ли это стенка
                    if (cellIndex == i + maze.HorizontalCells)
                    {
                        downWallIndex = cellIndex;
                    }

                    // проверяем, правая ли это стенка
                    if (cellIndex == i + 1)
                    {
                        rightWallIndex = cellIndex;
                    }

                    // проверяем, нижняя ли это стенка
                    if (cellIndex == i - maze.HorizontalCells)
                    {
                        upWallIndex = cellIndex;
                    }
                }

                // проверка на край карты
                if (currentCellInfo.LeftWall == -1)
                {
                    isLeftRemoved = false;
                }
                if (currentCellInfo.RightWall == -1)
                {
                    isRightRemoved = false;
                }
                if (currentCellInfo.TopWall == -1)
                {
                    isUpRemoved = false;
                }
                if (currentCellInfo.BottomWall == -1)
                {
                    isDownRemoved = false;
                }

                // проверка на удаленные зоны
                if(isLeftRemoved && leftWallIndex >= 0)
                {
                    var cell = mazeCells[leftWallIndex];
                    var cellInfo = GetComponent<MazeCell>(cell.Cell);
                    if(zonesInPath.Contains(cellInfo.ZoneId) == false)
                    {
                        isLeftRemoved = false;
                    }
                }
                if (isRightRemoved && rightWallIndex >= 0)
                {
                    var cell = mazeCells[rightWallIndex];
                    var cellInfo = GetComponent<MazeCell>(cell.Cell);
                    if (zonesInPath.Contains(cellInfo.ZoneId) == false)
                    {
                        isRightRemoved = false;
                    }
                }
                if (isUpRemoved && upWallIndex >= 0)
                {
                    var cell = mazeCells[upWallIndex];
                    var cellInfo = GetComponent<MazeCell>(cell.Cell);
                    if (zonesInPath.Contains(cellInfo.ZoneId) == false)
                    {
                        isUpRemoved = false;
                    }
                }
                if (isDownRemoved && downWallIndex >= 0)
                {
                    var cell = mazeCells[downWallIndex];
                    var cellInfo = GetComponent<MazeCell>(cell.Cell);
                    if (zonesInPath.Contains(cellInfo.ZoneId) == false)
                    {
                        isDownRemoved = false;
                    }
                }
                
                bool isRightBorderIndex = (i + 1) % maze.HorizontalCells == 0;
                bool isLeftBorderIndex = i % maze.HorizontalCells == 0;
                bool isDownBorderIndex = i + maze.HorizontalCells >= mazeCells.Length;
                bool isUpBorderIndex = i < maze.HorizontalCells;

                if (isLeftRemoved == false 
                    && (leftWallIndex == -1 || addedWalls.Contains(new AddedWallInfo(i, leftWallIndex)) == false))
                {
                    addedWalls.Add(new AddedWallInfo(i, leftWallIndex));

                    // спауним левую
                    wallSpawnLocation = currentCellLocation;
                    wallSpawnLocation.x += cellSize * 0.5f;

                    borderCellLocation = currentCellLocation;
                    borderCellLocation.x += cellSize;

                    
                    DynamicBuffer<WallPrefabs> selectedWallPrefabs;

                    if((isRightBorderIndex == false && isLeftBorderIndex) && borderWallPrefabs.IsEmpty == false)
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.blue, 999);
                        selectedWallPrefabs = borderWallPrefabs.Reinterpret<WallPrefabs>();
                    }
                    else
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.yellow, 999);
                        selectedWallPrefabs = wallPrefabs;
                    }

                    spawnWall(selectedWallPrefabs, wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);

                    if(isLeftBorderIndex)
                    {
                        isHorizontalBorderCell = true;
                        spawnBorderCell(borderCellPrefabs, borderCellLocation, 90, ref linkedEntities, ref random, ref commands);
                    }

                    addColumnPosition(true, wallSpawnLocation, cellSize, isLeftBorderIndex, columnPositions);
                }

                if (isUpRemoved == false && (upWallIndex == -1 || addedWalls.Contains(new AddedWallInfo(i, upWallIndex)) == false))//(i + maze.HorizontalCells >= mazeCells.Length)
                {
                    addedWalls.Add(new AddedWallInfo(i, upWallIndex));

                    // спауним верхнюю
                    wallSpawnLocation = currentCellLocation;
                    wallSpawnLocation.z += -cellSize * 0.5f;

                    borderCellLocation = currentCellLocation;
                    borderCellLocation.z += -cellSize;

                    DynamicBuffer<WallPrefabs> selectedWallPrefabs;

                    if ((isUpBorderIndex && isDownBorderIndex == false) && borderWallPrefabs.IsEmpty == false)
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.blue, 999);
                        selectedWallPrefabs = borderWallPrefabs.Reinterpret<WallPrefabs>();
                    }
                    else
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.yellow, 999);
                        selectedWallPrefabs = wallPrefabs;
                    }

                    spawnWall(selectedWallPrefabs, wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);

                    if(isUpBorderIndex)
                    {
                        isVerticalBorderCell = true;
                        spawnBorderCell(borderCellPrefabs, borderCellLocation, 180, ref linkedEntities, ref random, ref commands);
                    }

                    addColumnPosition(false, wallSpawnLocation, cellSize, isUpBorderIndex, columnPositions);
                }

                if (isRightRemoved == false && (rightWallIndex == -1 || addedWalls.Contains(new AddedWallInfo(i, rightWallIndex)) == false))
                {
                    addedWalls.Add(new AddedWallInfo(i, rightWallIndex));

                    // спауним правую
                    wallSpawnLocation = currentCellLocation;
                    wallSpawnLocation.x += -cellSize * 0.5f;

                    borderCellLocation = currentCellLocation;
                    borderCellLocation.x += -cellSize;
                    
                    DynamicBuffer<WallPrefabs> selectedWallPrefabs;

                    if ((isRightBorderIndex && isLeftBorderIndex == false) && borderWallPrefabs.IsEmpty == false)
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.blue, 999);
                        selectedWallPrefabs = borderWallPrefabs.Reinterpret<WallPrefabs>();
                    }
                    else
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.yellow, 999);
                        selectedWallPrefabs = wallPrefabs;
                    }

                    spawnWall(selectedWallPrefabs, wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);

                    if(isRightBorderIndex)
                    {
                        isHorizontalBorderCell = true;
                        spawnBorderCell(borderCellPrefabs, borderCellLocation, -90, ref linkedEntities, ref random, ref commands);
                    }

                    addColumnPosition(true, wallSpawnLocation, cellSize, isRightBorderIndex, columnPositions);
                }

                if (isDownRemoved == false && (downWallIndex == -1 || addedWalls.Contains(new AddedWallInfo(i, downWallIndex)) == false))
                {
                    addedWalls.Add(new AddedWallInfo(i, downWallIndex));

                    // спауним нижнюю
                    wallSpawnLocation = currentCellLocation;
                    wallSpawnLocation.z += cellSize * 0.5f;

                    borderCellLocation = currentCellLocation;
                    borderCellLocation.z += cellSize;

                    DynamicBuffer<WallPrefabs> selectedWallPrefabs;

                    if ((isDownBorderIndex && isUpBorderIndex == false) && borderWallPrefabs.IsEmpty == false)
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.blue, 999);
                        selectedWallPrefabs = borderWallPrefabs.Reinterpret<WallPrefabs>();
                    }
                    else
                    {
                        Debug.DrawRay(wallSpawnLocation,  Vector3.up, Color.yellow, 999);
                        selectedWallPrefabs = wallPrefabs;
                    }

                    spawnWall(selectedWallPrefabs, wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);

                    if(isDownBorderIndex)
                    {
                        isVerticalBorderCell = true;
                        spawnBorderCell(borderCellPrefabs, borderCellLocation, 0, ref linkedEntities, ref random, ref commands);
                    }

                    addColumnPosition(false, wallSpawnLocation, cellSize, isDownBorderIndex, columnPositions);
                }

                if (isHorizontalBorderCell && isVerticalBorderCell)
                {
                    borderCellLocation = currentCellLocation;
                    borderCellLocation.x += cellSize * (i == 0 || i == mazeCells.Length - maze.HorizontalCells ? 1 : -1);
                    borderCellLocation.z += cellSize * (i == mazeCells.Length - 1 || (i == mazeCells.Length - maze.HorizontalCells) ? 1 : -1);
                    spawnBorderCell(borderCellPrefabs, borderCellLocation, 0, ref linkedEntities, ref random, ref commands);

                    //if (isStartCell)
                    //{
                    //    if (isLeftRemoved == true)
                    //    {
                    //        wallSpawnLocation = currentCellLocation;
                    //        wallSpawnLocation.x -= cellSize * -0.5f;
                    //        spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);
                    //    }
                    //    if (isUpRemoved == true)
                    //    {
                    //        wallSpawnLocation = currentCellLocation;
                    //        wallSpawnLocation.z += cellSize * 0.5f;
                    //        spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);
                    //    }
                    //    if (isRightRemoved == true)
                    //    {
                    //        wallSpawnLocation = currentCellLocation;
                    //        wallSpawnLocation.x += cellSize * -0.5f;
                    //        spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);
                    //    }
                    //    if (isDownRemoved == true)
                    //    {
                    //        wallSpawnLocation = currentCellLocation;
                    //        wallSpawnLocation.z += -cellSize * 0.5f;
                    //        spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);
                    //    }
                    //}
                }

                // спауним стены с дверьми, разделяющими разные зоны
                doorProcessedCells.Add(i);

                foreach (var removedWall in currentCellRemovedWalls)
                {
                    if (doorProcessedCells.Contains(removedWall.CellIndex))
                    {
                        // пропускаем эту ячейку, так как для нее уже создавались двери
                        continue;
                    }

                    var neighbourCellElement = mazeCells[removedWall.CellIndex];
                    var neighbourCellInfo = GetComponent<MazeCell>(neighbourCellElement.Cell);

                    // пропускаем эту ячейку, если она находится в этой же зоне
                    if (neighbourCellInfo.ZoneId == currentCellInfo.ZoneId)
                    {
                        continue;
                    }

                    if (isLeftRemoved && leftWallIndex == removedWall.CellIndex)
                    {
                        wallSpawnLocation = currentCellLocation;
                        wallSpawnLocation.x -= cellSize * -0.5f;
                        var wallEntity = spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);
                        if(wallEntity != Entity.Null)
                        {
                            commands.SetComponentWithDefaultKey(wallEntity, new ZoneGate { Zone1 = new ZoneId(currentCellInfo.ZoneId), Zone2 = new ZoneId(neighbourCellInfo.ZoneId) });
                        }
                    }
                    if (isUpRemoved && upWallIndex == removedWall.CellIndex)
                    {
                        wallSpawnLocation = currentCellLocation;
                        wallSpawnLocation.z += -cellSize * 0.5f;
                        var wallEntity = spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);
                        if (wallEntity != Entity.Null)
                        {
                            commands.SetComponentWithDefaultKey(wallEntity, new ZoneGate { Zone1 = new ZoneId(currentCellInfo.ZoneId), Zone2 = new ZoneId(neighbourCellInfo.ZoneId) });
                        }
                    }
                    if (isRightRemoved && rightWallIndex == removedWall.CellIndex)
                    {
                        wallSpawnLocation = currentCellLocation;
                        wallSpawnLocation.x += cellSize * -0.5f;
                        var wallEntity = spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, true, ref linkedEntities, ref random, ref commands);
                        if (wallEntity != Entity.Null)
                        {
                            commands.SetComponentWithDefaultKey(wallEntity, new ZoneGate { Zone1 = new ZoneId(currentCellInfo.ZoneId), Zone2 = new ZoneId(neighbourCellInfo.ZoneId) });
                        }
                    }
                    if (isDownRemoved && downWallIndex == removedWall.CellIndex)
                    {
                        wallSpawnLocation = currentCellLocation;
                        wallSpawnLocation.z += cellSize * 0.5f;
                        var wallEntity = spawnWall(doorWallPrefabs.Reinterpret<WallPrefabs>(), wallSpawnLocation, false, ref linkedEntities, ref random, ref commands);
                        if (wallEntity != Entity.Null)
                        {
                            commands.SetComponentWithDefaultKey(wallEntity, new ZoneGate { Zone1 = new ZoneId(currentCellInfo.ZoneId), Zone2 = new ZoneId(neighbourCellInfo.ZoneId) });
                        }
                    }
                }
            }

            // спауним окружение
            var environmentPrefabs = GetBuffer<EnvironmentPrefabs>(mazeBuilder);
            if (environmentPrefabs.Length > 0)
            {
                var envPrefab = environmentPrefabs[random.NextInt(0, environmentPrefabs.Length)];
                var envInstance = commands.InstantiateWithDefaultKey(envPrefab.Prefab);
                linkedEntities.Add(new MazeObjects { Value = envInstance });
            }

            // туман
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fog = true;
            RenderSettings.fogColor = builderData.FogColor;
            RenderSettings.fogStartDistance = builderData.FogStart;
            RenderSettings.fogEndDistance = builderData.FogEnd;
            
            // освещение
            RenderSettings.subtractiveShadowColor = builderData.RelatimeShadowColor;
            
            // спауним точку старта
            //var spawnPoint = new GameObject("Player spawn point");
            //spawnPoint.transform.SetParent(mazeContainer);
            //spawnPoint.transform.position = StartLocation + startHeightTranslation;
            //playerSpawnPoint = spawnPoint;//.AddComponent<EndlessPlayerSpawnPoint>();

            // спауним меш для точки старта
            //StartPointActor = Spawn(class'FB_RandomMaze_StaticMesh', self, , StartLocation);
            //FB_RandomMaze_StaticMesh(StartPointActor).Mesh.SetStaticMesh(StartPointMesh);
            //FB_RandomMaze_StaticMesh(StartPointActor).Mesh.SetMaterial(0, MaterialInstanceConstant'Check_Points.Materials.M_Start_Point_1_INST');

            // спауним тригер финиша
            //if (finishPrefab != null)
            //{
            //    finishPoint = Instantiate(finishPrefab, FinishLocation, Quaternion.identity, mazeContainer);
            //}

            // спауним пол ------------------

            // определяем размер префаба пола
            ////var tempFloor = Instantiate(FloorMesh, StartLocation + float3.down * 1000, Quaternion.identity, mazeContainer);
            ////var tempFloorMesh = tempFloor.GetComponentInChildren<Renderer>();
            ////var floorBounds = tempFloorMesh.bounds;

            ////destroyThing(tempFloor.gameObject);

            //// считаем начальный размер меша пола
            ////float3 floorSize, mazeSize;

            ////floorSize.x = Mathf.Abs(floorBounds.max.x - floorBounds.min.x);
            ////floorSize.y = Mathf.Abs(floorBounds.max.y - floorBounds.min.y);
            ////floorSize.z = Mathf.Abs(floorBounds.max.z - floorBounds.min.z);


            //// считаем размер лабиринта и пола
            ////mazeSize.x = Mathf.Abs(MazeGlobalCorner1.x) + Mathf.Abs(MazeGlobalCorner2.x);
            ////mazeSize.y = Mathf.Abs(MazeGlobalCorner1.y) + Mathf.Abs(MazeGlobalCorner2.y);
            ////mazeSize.z = Mathf.Abs(MazeGlobalCorner1.z) + Mathf.Abs(MazeGlobalCorner2.z);

            ////// считаем соотношение
            ////int floorCount_X = Mathf.CeilToInt(mazeSize.x / floorSize.x);
            ////int floorCount_Z = Mathf.CeilToInt(mazeSize.z / floorSize.z);
            //////floorScaleFactor.y = 1.0f;

            ////float3 floorLoc = float3.zero;
            ////floorLoc.y = MazeGlobalCorner1.y - floorSize.y + startHeightTranslation.y;

            ////for (int x = 0; x < floorCount_X; x++)
            ////{
            ////    for (int z = 0; z < floorCount_Z; z++)
            ////    {
            ////        floorLoc.x = MazeGlobalCorner2.x + x * floorSize.x + floorSize.x * 0.5f;
            ////        floorLoc.z = MazeGlobalCorner2.z - z * floorSize.z - floorSize.z * 0.5f;

            ////        var floor = Instantiate(FloorMesh, floorLoc, Quaternion.identity, mazeContainer);
            ////    }
            ////}
            ///

            // спауним углы стен
            Debug.Log($"Spawning columns: {columnPositions.Length}");

            foreach (var pos in columnPositions)
            {
                Entity columnPrefab;

                if(pos.IsBorder && borderColumnPrefabs.Length > 0)
                {
                    //Debug.DrawRay(pos.Position + (float3)Vector3.down, Vector3.down, Color.red, 9999);
                    columnPrefab = borderColumnPrefabs[random.NextInt(0, borderColumnPrefabs.Length)].Prefab;
                }
                else
                {
                    //Debug.DrawRay(pos.Position, Vector3.down, Color.yellow, 9999);
                    columnPrefab = columnPrefabs[random.NextInt(0, columnPrefabs.Length)].Prefab;
                }

                var column = commands.InstantiateWithDefaultKey(columnPrefab);
                var lt = LocalTransform.FromPosition(pos.Position);
                //lt = LocalTransform.FromPositionRotation(pos, quaternion.EulerXYZ(UnityEngine.Random.Range(-90, 90), 0, UnityEngine.Random.Range(-90, 90)));
                commands.SetComponentWithDefaultKey(column, lt);
                linkedEntities.Add(new MazeObjects { Value = column });
            }

            //Debug.Log("Walls count: " + walls.Count);
        }

        void addColumnPosition(bool horizontal, float3 wallPos, float cellSize, bool isBorder, NativeList<ColumnPositionInfo> columnPositions)
        {
            var shift = (horizontal ? new float3(0, 0, cellSize) : new float3(cellSize, 0, 0));
            shift = shift * 0.5f;

            var p1 = new ColumnPositionInfo { Position = wallPos + shift, IsBorder = isBorder };
            var p2 = new ColumnPositionInfo { Position = wallPos - shift, IsBorder = isBorder };

            if (columnPositions.Contains(p1) == false)
            {
                columnPositions.Add(p1);
            }

            if (columnPositions.Contains(p2) == false)
            {
                columnPositions.Add(p2);
            }
        }

        // генерация списка стартовых ячеек (например, если нужно несколько стартовых ячеек для всех игроков)
        static List<StartCellInfo> initStartCells(Maze maze, ref DynamicBuffer<MazeCells> mazeCells)
        {
            var startCellCount = maze.StartCellCount;
            if (startCellCount > mazeCells.Length)
            {
                Debug.LogError($"Invalid start cell count {startCellCount}");
                startCellCount = mazeCells.Length;
            }
            if (startCellCount == 0)
            {
                Debug.LogError($"Invalid start cell count {startCellCount}");
                startCellCount = 1;
            }
            var startCellList = new List<StartCellInfo>(startCellCount);

            int perimeterCellCount = (maze.VerticalCells + maze.HorizontalCells) * 2 - 4;
            float averageCellDistance = perimeterCellCount / startCellCount;
            int startCellNumber = 1;
            int currentEdgeCellCounter = 0;
            int currentStartCells = 0;

            System.Action<int, int> processCellEdge = (currentCellIndex, sideIndex) =>
            {
                // краевая ячейка
                //Debug.Log($"edge cell {currentCellIndex}");

                if (currentStartCells == 0)
                {
                    if (currentEdgeCellCounter == startCellNumber)
                    {
                        startCellList.Add(new StartCellInfo { Index = currentCellIndex, SideIndex = sideIndex });
                        currentStartCells++;
                        //Debug.Log($"Adding {currentCellIndex}");
                    }
                }
                else
                {
                    int distance = Mathf.RoundToInt(averageCellDistance * currentStartCells);
                    if (distance + 1 == currentEdgeCellCounter)
                    {
                        //Debug.Log($"Adding {currentCellIndex}, edge counter: {currentEdgeCellCounter}");
                        startCellList.Add(new StartCellInfo { Index = currentCellIndex, SideIndex = sideIndex });
                        currentStartCells++;
                    }
                }

                currentEdgeCellCounter++;
            };

            // bottom horizontal
            for (int i = 0; i < maze.HorizontalCells; i++)
            {
                processCellEdge(i, 0);
            }
            // right vertical
            for (int i = 1; i < maze.VerticalCells; i++)
            {
                processCellEdge(i * maze.HorizontalCells + maze.HorizontalCells - 1, 1);
            }
            // top horizontal
            for (int i = maze.HorizontalCells - 2; i >= 0; i--)
            {
                processCellEdge(i + maze.VerticalCells * (maze.HorizontalCells - 1), 2);
            }
            // left vertical
            for (int i = maze.VerticalCells - 2; i >= 1; i--)
            {
                processCellEdge(i * maze.HorizontalCells, 3);
            }

            return startCellList;
        }

        struct MazeCellInfo
        {
            public Entity Entity;
            public MazeCell MazeCell;
            public DynamicBuffer<MazeCellNeighbors> Neighbors;
            public DynamicBuffer<MaceCellRemovedWalls> RemovedWalls;
            public DynamicBuffer<MazeCellWalls> Walls;
        }

        [BurstCompile]
        void GenerateMaze(Entity mazeEntity, Entity builder, uint seed, int horizontalCells, int verticalCells, int startCellCount, ref Random random, ref UniversalCommandBuffer commands)
        {
            var cellNum = horizontalCells * verticalCells;

            var cells = new List<MazeCellInfo>(cellNum);
            var visitChain = new NativeList<int>(cellNum, Allocator.Temp);

            var maze = new Maze
            {
                Builder = builder,
                Seed = seed,
                HorizontalCells = horizontalCells,
                VerticalCells = verticalCells,
                StartCellCount = startCellCount
            };

            var mazeCellsBuffer = commands.AddBufferWithDefaultKey<MazeCells>(mazeEntity);

            // создаем сущности ячеек
            for (int i = 0; i < horizontalCells; i++)
            {
                for (int j = 0; j < verticalCells; j++)
                {
                    var cellEntity = commands.CreateEntityWithDefaultKey(cellArchetype);
                    mazeCellsBuffer.Add(new MazeCells 
                    { 
                        Cell = cellEntity
                    });
                }
            }

            // рассчитываем позиции ячеек
            var builderData = EntityManager.GetComponentData<MazeWorldBuilder>(builder);
            var cellSize = builderData.CellSize;
            var mazeStartOffset = maze.CalculateWorldStartOffset(cellSize);
            int zCount = 0;

            for (int i=0; i<mazeCellsBuffer.Length; i++)
            {
                var mcell = mazeCellsBuffer[i];
                mcell.Position = calculateCellPosition(i, maze.HorizontalCells, cellSize, in mazeStartOffset, ref zCount);
                mazeCellsBuffer[i] = mcell;
            }

            int startCellIndex = 0;// random.NextInt(0, cellNum);

            var currentCellNumber = startCellIndex;

            // заполняем массив ячеек лабиринта информацией о соседних ячейках
            // нумерация ячеек идет слева направа, сверху вниз
            for (int i = 0; i < cellNum; i++)
            {
                var mazeCellElement = mazeCellsBuffer[i];

                var mazeCellInfo = new MazeCellInfo
                {
                    MazeCell = MazeCell.Default,
                    Entity = mazeCellElement.Cell,
                    Neighbors = commands.SetBufferWithDefaultKey<MazeCellNeighbors>(mazeCellElement.Cell),
                    RemovedWalls = commands.SetBufferWithDefaultKey<MaceCellRemovedWalls>(mazeCellElement.Cell),
                    Walls = commands.SetBufferWithDefaultKey<MazeCellWalls>(mazeCellElement.Cell)
                };


                mazeCellInfo.MazeCell.Num = i;

                // по умолчанию - край карты
                mazeCellInfo.MazeCell.BottomWall = -1;
                mazeCellInfo.MazeCell.TopWall = -1;
                mazeCellInfo.MazeCell.RightWall = -1;
                mazeCellInfo.MazeCell.LeftWall = -1;

                // добавляем нижнего соседа
                if (i + maze.HorizontalCells < cellNum)
                {
                    mazeCellInfo.MazeCell.BottomWall = i + maze.HorizontalCells;
                    mazeCellInfo.Neighbors.Add(new MazeCellNeighbors { CellIndex = mazeCellInfo.MazeCell.BottomWall });
                    mazeCellInfo.Walls.Add(new MazeCellWalls { CellIndex = mazeCellInfo.MazeCell.BottomWall });
                }

                // добавляем правого соседа
                if ((i + 1) % maze.HorizontalCells != 0)
                {
                    mazeCellInfo.MazeCell.RightWall = i + 1;
                    mazeCellInfo.Neighbors.Add(new MazeCellNeighbors { CellIndex = mazeCellInfo.MazeCell.RightWall });
                    mazeCellInfo.Walls.Add(new MazeCellWalls { CellIndex = mazeCellInfo.MazeCell.RightWall });
                }

                // добавляем левого соседа
                if (i % maze.HorizontalCells != 0)
                {
                    mazeCellInfo.MazeCell.LeftWall = i - 1;
                    mazeCellInfo.Neighbors.Add(new MazeCellNeighbors { CellIndex = mazeCellInfo.MazeCell.LeftWall });
                    mazeCellInfo.Walls.Add(new MazeCellWalls { CellIndex = mazeCellInfo.MazeCell.LeftWall });
                }

                // добавляем верхнего соседа
                if (i - maze.HorizontalCells >= 0)
                {
                    mazeCellInfo.MazeCell.TopWall = i - maze.HorizontalCells;
                    mazeCellInfo.Neighbors.Add(new MazeCellNeighbors { CellIndex = mazeCellInfo.MazeCell.TopWall });
                    mazeCellInfo.Walls.Add(new MazeCellWalls { CellIndex = mazeCellInfo.MazeCell.TopWall });
                }

                if (i == currentCellNumber)
                {
                    mazeCellInfo.MazeCell.IsVisited = true;
                }

                cells.Add(mazeCellInfo);
            }


            // добавляем уже выбранную ячейку
            visitChain.Add(startCellIndex);

            while (visitChain.Length != 0)
            {
                currentCellNumber = visitChain[visitChain.Length - 1];
                var currentCell = cells[currentCellNumber];

                if (currentCell.Neighbors.Length > 0)
                {
                    // случайно выбираем соседнюю ячейку
                    var randomNeighbourNumber = random.NextInt(0, currentCell.Neighbors.Length);

                    var randomCellNumber = currentCell.Neighbors[randomNeighbourNumber].CellIndex;
                    var randomCell = cells[randomCellNumber];

                    // если ячейка еще не посещена, добавляем ее в цепь
                    if (randomCell.MazeCell.IsVisited == false)
                    {
                        randomCell.MazeCell.IsVisited = true;
                        visitChain.Add(randomCellNumber);

                        // добавляем проход между ячейками
                        currentCell.RemovedWalls.Add(new MaceCellRemovedWalls { CellIndex = randomCellNumber });
                        randomCell.RemovedWalls.Add(new MaceCellRemovedWalls { CellIndex = currentCellNumber });

                        // удаляем стену из списка
                        for (int i = 0; i < currentCell.Walls.Length; i++)
                        {
                            var wallElem = currentCell.Walls[i];

                            if (wallElem.CellIndex == randomCellNumber)
                            {
                                currentCell.Walls.RemoveAt(i);
                                break;
                            }
                        }

                        for (int i = 0; i < randomCell.Walls.Length; i++)
                        {
                            var elem = randomCell.Walls[i];

                            if (elem.CellIndex == currentCellNumber)
                            {
                                randomCell.Walls.RemoveAt(i);
                                break;
                            }
                        }
                        //randomCellWalls.Remove(currentCellNumber);

                        // current
                        if (currentCell.MazeCell.LeftWall == randomCellNumber)
                        {
                            currentCell.MazeCell.LeftWall = -2;
                        }
                        else if (currentCell.MazeCell.RightWall == randomCellNumber)
                        {
                            currentCell.MazeCell.RightWall = -2;
                        }
                        else if (currentCell.MazeCell.TopWall == randomCellNumber)
                        {
                            currentCell.MazeCell.TopWall = -2;
                        }
                        else if (currentCell.MazeCell.BottomWall == randomCellNumber)
                        {
                            currentCell.MazeCell.BottomWall = -2;
                        }

                        cells[currentCellNumber] = currentCell;

                        // random
                        if (randomCell.MazeCell.LeftWall == currentCellNumber)
                        {
                            randomCell.MazeCell.LeftWall = -2;
                        }
                        else if (randomCell.MazeCell.RightWall == currentCellNumber)
                        {
                            randomCell.MazeCell.RightWall = -2;
                        }
                        else if (randomCell.MazeCell.TopWall == currentCellNumber)
                        {
                            randomCell.MazeCell.TopWall = -2;
                        }
                        else if (randomCell.MazeCell.BottomWall == currentCellNumber)
                        {
                            randomCell.MazeCell.BottomWall = -2;
                        }

                        cells[randomCellNumber] = randomCell;
                    }

                    // удаляем соседа
                    currentCell.Neighbors.RemoveAt(randomNeighbourNumber);
                }
                else
                {
                    // если у ячейки нет соседей, удаляем ее из цепи
                    visitChain.RemoveAt(visitChain.Length - 1);
                }
            }


            // генерация зон

            // счетчик ячеек в текущей зоне. 
            int zoneCellCounter = 0;

            // текущий индекс зоны (0 - не присвоен)
            ushort zoneId = 1;

            // назначаем индексы зон
            for(int i=0; i<cells.Count; i++)
            {
                var cell = cells[i];
                cell.MazeCell.IsVisited = false;
                cells[i] = cell;
            }

            visitChain.Add(startCellIndex);

            while(visitChain.Length != 0)
            {
                var currentCellIndex = visitChain[visitChain.Length - 1];
                var currentCell = cells[currentCellIndex];

                // назначаем текущей ячейке идентификатор зоны
                if(currentCell.MazeCell.IsVisited == false)
                {
                    currentCell.MazeCell.IsVisited = true;
                    currentCell.MazeCell.ZoneId = zoneId;

                    if(currentCellIndex == startCellIndex && zoneId == 1)
                    {
                        // зона 0 - только для стартовой ячейки
                        zoneId++;
                    }

                    zoneCellCounter++;

                    cells[currentCellIndex] = currentCell;
                }
                
                // выбираем следующую смежную ячейку, которая еще не была посещена
                int nextCellIndex = -1;

                foreach(var removedWall in currentCell.RemovedWalls)
                {
                    var nextCell = cells[removedWall.CellIndex];

                    if(nextCell.MazeCell.IsVisited == false)
                    {
                        nextCellIndex = removedWall.CellIndex;
                        break;
                    }
                }

                // если не нашлось подходящих соседних ячеек - значит тупик,
                // поэтому следующая зона должна начинаться с нового индекса
                if(nextCellIndex == -1)
                {
                    zoneId++;
                    zoneCellCounter = 0;

                    visitChain.RemoveAt(visitChain.Length - 1);
                    continue;
                }

                // если подходящая ячейка найдена, добавляем ее в цепь пути
                visitChain.Add(nextCellIndex);

                // если в текущей зоны набралось достаточное количество ячеек, то "закрываем" зону
                // и последующие ячейки будут назначаться в новую зону
                if (zoneCellCounter >= 5)
                {
                    zoneId++;
                    zoneCellCounter = 0;
                }
            }

            // записываем результаты
            commands.AddComponentWithDefaultKey(mazeEntity, maze);

            foreach(var cell in cells)
            {
                commands.SetComponentWithDefaultKey(cell.Entity, cell.MazeCell);
            }
        }
    }
}
