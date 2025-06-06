using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct TerrainDetailGenerationSettings : IComponentData
    {
        public Entity Prefab;
        public float CellSize;
        public float SpawnRadius;
        [Range(0,1)]
        public float RelaxFactor;
        [Range(0,1)]
        public float Density;
        [HideInAuthoring]
        public CollisionFilter TraceLayers;
        public float TraceVerticalOffset;
        [HideInAuthoring] public byte PhysicsMaterialTags;
        public float MinimalLayerStrength;
        public float MinScale;
        public float MaxScale;
    }
    
    [UseDefaultInspector(true)]
    public class TerrainDetailComponent : ComponentDataBehaviour<TerrainDetailGenerationSettings>
    {
        public TerrainDetailCellComponent Prefab;
        public LayerMask TraceLayers;
        public CustomPhysicsMaterialTags PhysicsMaterialTags;

        protected override void Bake<K>(ref TerrainDetailGenerationSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.Prefab = baker.GetEntity(Prefab);
            serializedData.TraceLayers = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = Utility.LayerMaskToCollidesWithMask(TraceLayers)
            };
            serializedData.PhysicsMaterialTags = PhysicsMaterialTags.Value;
        }

        protected override TerrainDetailGenerationSettings CreateDefaultValue()
        {
            return new TerrainDetailGenerationSettings
            {
                CellSize = 1,
                RelaxFactor = 0.5f,
                SpawnRadius = 20,
                Density = 0.5f,
                MinScale = 1,
                MaxScale = 1,
                MinimalLayerStrength = 1f,
                TraceVerticalOffset = 10
            };
        }
    }
}
