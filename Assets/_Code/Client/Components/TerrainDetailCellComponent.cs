using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Arena.Client
{
    [Serializable]
    public struct TerrainDetailCell : IComponentData
    {
        [HideInAuthoring] public uint Hash;
        [HideInAuthoring] public float2 WorldPosition;

        public static bool CheckDensity(uint cellHash, float density)
        {
            return CheckDensity(cellHash, density, out _);
        }
        
        public static bool CheckDensity(uint cellHash, float density, out Random random)
        {
            random = Random.CreateFromIndex(cellHash);
            var densityPass = random.NextFloat(0, 1);
            if (densityPass > density)
            {
                return false;
            }
            return true;
        }
    }

    [Serializable]
    [ChunkSerializable]
    public struct TerrainDetailCellSharedData : ISharedComponentData, IEquatable<TerrainDetailCellSharedData>
    {
        private Entity settingsEntity;

        public TerrainDetailCellSharedData(Entity settingsEntity)
        {
            this.settingsEntity = settingsEntity;
        }

        public bool Equals(TerrainDetailCellSharedData other)
        {
            return settingsEntity.Equals(other.settingsEntity);
        }
        public bool Equals(Entity other)
        {
            return settingsEntity.Equals(other);
        }

        public override bool Equals(object obj)
        {
            return obj is TerrainDetailCellSharedData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return settingsEntity.GetHashCode();
        }
    }

    public class TerrainDetailCellComponent : ComponentDataBehaviour<TerrainDetailCell>
    {
        protected override void Bake<K>(ref TerrainDetailCell serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddSharedComponent(new TerrainDetailCellSharedData());
        }
    }
}