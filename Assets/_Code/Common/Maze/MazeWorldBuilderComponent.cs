using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using TzarGames.GameCore;

namespace Arena.Maze
{
    public struct MazeWorldBuilder : IComponentData
    {
        public float CellSize;
        public byte LocationID;
        public Entity ZonePrefab;

        [Header("Fog settings")]
        public Color FogColor;
        public float FogStart;
        public float FogEnd;

        [Header("Lighting settings")] public Color RelatimeShadowColor;
    }

    [System.Serializable]
    public struct BorderWallPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct DoorWallPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct EnvironmentPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct WallPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct StartCellPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct FinishCellPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [System.Serializable]
    public struct GameCellPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }
    [System.Serializable]
    public struct BorderCellPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }
    [System.Serializable]
    public struct EnvironmentCellPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }
    [System.Serializable]
    public struct ColumnPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }
    [System.Serializable]
    public struct BorderColumnPrefabs : IBufferElementData
    {
        public Entity Prefab;
    }

    [UseDefaultInspector]
    public class MazeWorldBuilderComponent : ComponentDataBehaviour<MazeWorldBuilder>
    {
        [SerializeField]
        LocationID locationID = default;

        [SerializeField] private ZoneIdComponent zonePrefab;

        [SerializeField]
        float cellSize = 10;

        [Header("Fog settings")]
        [SerializeField]
        Color fogColor = Color.cyan;

        [SerializeField]
        float fogStart = 15;

        [SerializeField]
        float fogEnd = 200;

        [Header("Lighting settings")]
        [SerializeField]
        [ColorUsage(false, true)]
        Color ambientSkyColor = Color.white;

        [SerializeField]
        [ColorUsage(false, true)]
        Color ambientEquatorColor = Color.gray;

        [SerializeField]
        [ColorUsage(false, true)]
        Color ambientGroundColor = Color.black;

        [SerializeField]
        private Color realtimeShadowColor = new Color(0.3f, 0.3f, 0.3f);

        abstract class MazeEntryInfo
        {
            public GameObject Prefab;
        }
        
        [Serializable]
        class MazeCellInfo : MazeEntryInfo
        {
        }

        [Serializable]
        class MazeColumnInfo : MazeEntryInfo
        {
        }

        [Serializable]
        class MazeWallInfo : MazeEntryInfo
        {
        }

        [Serializable]
        class MazeEnvironmentInfo : MazeEntryInfo
        {
        }
        
        [Header("Maze builder elements")]
        [SerializeField]
        MazeCellInfo[] startCellPrefabs;

        [SerializeField]
        MazeCellInfo[] finishCellPrefabs;

        [SerializeField]
        MazeCellInfo[] gameCellPrefabs;
        [SerializeField]
        MazeCellInfo[] borderCellPrefabs;
        [SerializeField]
        MazeCellInfo[] environmentCellPrefabs;

        [SerializeField]
        MazeWallInfo[] wallPrefabs;

        [SerializeField]
        MazeWallInfo[] borderWallPrefabs;

        [SerializeField]
        MazeWallInfo[] doorWallPrefabs;

        [SerializeField]
        MazeColumnInfo[] columnPrefabs;

        [SerializeField]
        MazeColumnInfo[] borderColumnPrefabs;

        [SerializeField]
        MazeEnvironmentInfo[] environmentPrefabs;

        static void convertArray<T>(IGCBaker baker, MazeEntryInfo[] entries, System.Func<Entity, T> makeArrayElementFunc) where T : unmanaged, IBufferElementData
        {
            var buffer = baker.AddBuffer<T>();
            foreach (var entry in entries)
            {
                if(entry == null)
                {
                    continue;
                }
                var elem = makeArrayElementFunc(baker.GetEntity(entry.Prefab));
                buffer.Add(elem);
            }
        }

        protected override void Bake<K>(ref MazeWorldBuilder serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            serializedData = new MazeWorldBuilder
            {
                CellSize = cellSize,
                LocationID = (byte)(locationID != null ? locationID.Id : 0),
                FogColor = fogColor,
                FogStart = fogStart,
                FogEnd = fogEnd,
                RelatimeShadowColor = realtimeShadowColor,
                ZonePrefab = baker.GetEntity(zonePrefab)
            };

            convertArray(baker, startCellPrefabs, (prefabEntity) => new StartCellPrefabs { Prefab = prefabEntity });
            convertArray(baker, finishCellPrefabs, (prefabEntity) => new FinishCellPrefabs { Prefab = prefabEntity });
            convertArray(baker, gameCellPrefabs, (prefabEntity) => new GameCellPrefabs { Prefab = prefabEntity });
            convertArray(baker, borderWallPrefabs, (prefabEntity) => new BorderWallPrefabs { Prefab = prefabEntity });
            convertArray(baker, wallPrefabs, (prefabEntity) => new WallPrefabs { Prefab = prefabEntity });
            convertArray(baker, columnPrefabs, (prefabEntity) => new ColumnPrefabs { Prefab = prefabEntity });
            convertArray(baker, borderColumnPrefabs, (prefabEntity) => new BorderColumnPrefabs { Prefab = prefabEntity });
            convertArray(baker, environmentPrefabs, (prefabEntity) => new EnvironmentPrefabs { Prefab = prefabEntity });
            convertArray(baker, borderCellPrefabs, (prefabEntity) => new BorderCellPrefabs { Prefab = prefabEntity });
            convertArray(baker, doorWallPrefabs, (prefabEntity) => new DoorWallPrefabs { Prefab = prefabEntity });
            convertArray(baker, environmentCellPrefabs, (prefabEntity) => new EnvironmentCellPrefabs { Prefab = prefabEntity });
        }
    }
}

