using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Arena.Maze
{
    //public class TG_MazeCell
    //{
    //    public int Num;                        // номер ячейки
    //    //public List<int> Neighbors;           // номера соседних ячеек
    //    //public List<int> RemovedWalls;        // номера ячеек, к которым открыт проход (нет стены)
    //    //public List<int> Walls;               // номера ячеек, к которым закрыт проход

    //    // признак того, что ячейка посещена
    //    //public Vector3 Position;
    //};

    // номера соседних ячеек
    public struct MazeCellNeighbors : IBufferElementData
    {
        public int CellIndex;
    }

    // номера ячеек, к которым открыт проход (нет стены)
    public struct MaceCellRemovedWalls : IBufferElementData
    {
        public int CellIndex;
    }

    // номера ячеек, к которым закрыт проход
    public struct MazeCellWalls : IBufferElementData
    {
        public int CellIndex;
    }

    public struct MazeCell : IComponentData
    {
        public int Num;

        // -1 - край карты, -2 - удаленная стен
        public int LeftWall;
        public int TopWall;
        public int RightWall;
        public int BottomWall;

        public ushort ZoneId;

        public bool IsVisited;

        public static readonly MazeCell Default = new MazeCell
        {
            BottomWall = 1,
            LeftWall = -1,
            RightWall = -1,
            TopWall = -1
        };
    }

    [TzarGames.MultiplayerKit.Sync(priority: 90)]
    public struct MazeNetSync : IComponentData
    {
        public byte LocationID;
        public uint Seed;
        public int HorizontalCells;
        public int VerticalCells;
        public int StartCellCount;
    }

    public struct Maze : IComponentData
    {
        public Entity Builder;
        public uint Seed;
        public int HorizontalCells;
        public int VerticalCells;
        public int StartCellCount;
        internal ushort LastZone;

        public float2 CalculateWorldSize(float cellSize) => new float2((HorizontalCells) * cellSize, (VerticalCells) * cellSize);
        public float3 CalculateWorldStartOffset(float cellSize)
        {
            var worldSize = CalculateWorldSize(cellSize);
            return new float3(worldSize.x * 0.5f, 0, -worldSize.y * 0.5f);
        }
    }
    public struct MazeCells : ICleanupBufferElementData
    {
        public Entity Cell;
        public float3 Position;
    }

    // Генератор лабиринтов
    public static class MazeGenerator
    {
        
        //static private void dumpMazeInfo(Entity mazeEntity, EntityManager manager)
        //{
        //    int i, j;
        //    var str = new System.Text.StringBuilder();

        //    for (i = 0; i < mazeCells.Count; i++)
        //    {
        //        str.AppendFormat("Cell {0} Removed: (", i);

        //        for (j = 0; j < mazeCells[i].RemovedWalls.Count; j++)
        //        {
        //            str.Append(mazeCells[i].RemovedWalls[j] + ",");
        //        }

        //        str.Append(")");

        //        str.Append("Walls: (");

        //        for (j = 0; j < mazeCells[i].Walls.Count; j++)
        //        {
        //            str.Append(mazeCells[i].Walls[j] + ",");
        //        }

        //        str.Append(")\n");
        //    }
        //}
        
        // находит длиннейший путь в лабиринте. Если путь не найден - возвращается false
        public static bool FindLongestPath(Maze maze, int firstCell, int secondCell, List<int> Path)
        {
            Debug.LogError("Not implemented");
            return false;

            //Satsuma.CustomGraph graph = new Satsuma.CustomGraph();
            ////local TG_Graph_Vertex v;
            //int i, j, temp;
            //var nodesToCells = new Dictionary<long, int>();
            //var cellToNodes = new Dictionary<int, Satsuma.Node>();

            //// заполняем массив графов в соответствии с ячейками лабиринта

            //for (i = 0; i < maze.Cells.Count; i++)
            //{
            //    var node = graph.AddNode();
            //    nodesToCells.Add(node.Id, i);

            //    if(cellToNodes.ContainsKey(i) == false)
            //    {
            //        cellToNodes.Add(i, node);
            //    }

            //    for (j = 0; j < maze.Cells[i].RemovedWalls.Count; j++)
            //    {
            //        temp = maze.Cells[i].RemovedWalls[j];

            //        if(cellToNodes.ContainsKey(temp) == false)
            //        {
            //            var tmpNode = graph.AddNode();

            //            if(nodesToCells.ContainsKey(tmpNode.Id) == false)
            //            {
            //                nodesToCells.Add(tmpNode.Id, temp);
            //            }
                        
            //            if(cellToNodes.ContainsKey(temp) == false)
            //            {
            //                cellToNodes.Add(temp, tmpNode);
            //            }
            //        }
            //        graph.AddArc(cellToNodes[i], cellToNodes[temp], Satsuma.Directedness.Undirected);
                    
            //        //graph.Vertexes[i].LinkedVertexes.AddItem(temp);
            //        //graph.Vertexes[temp].LinkedVertexes.AddItem(i);
            //    }
            //}

            ////class'TG_GraphLibrary'.static.FindLongestPath(graph, firstCell, secondCell, gpath);
            //var dijkstra = new Satsuma.Dijkstra(graph, arc =>
            //{
            //    var source = graph.U(arc);
            //    var target = graph.V(arc);

            //    var sourceCellId = nodesToCells[source.Id];
            //    var targetCellId = nodesToCells[target.Id];

            //    var sourcePos = maze.Cells[sourceCellId].Position;
            //    var targetPos = maze.Cells[targetCellId].Position;

            //    return Vector3.SqrMagnitude(targetPos - sourcePos);

            //}, Satsuma.DijkstraMode.Sum);

            //var startNode = cellToNodes[firstCell];
            //var endNode = cellToNodes[secondCell];

            //dijkstra.AddSource(startNode);
            //dijkstra.RunUntilFixed(endNode);

            //var path = dijkstra.GetPath(endNode);
            //if (path == null)
            //{
            //    return false;
            //}

            //foreach (var n in path.Nodes())
            //{
            //    Path.Add(nodesToCells[n.Id]);
            //}
            
            //return true;
        }
    }
}
