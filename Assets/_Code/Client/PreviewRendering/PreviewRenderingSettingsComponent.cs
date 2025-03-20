using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.PreviewRendering
{
    [System.Serializable]
    public struct PreviewRenderingSettings : IComponentData
    {
        public Entity ItemPivot;
        public float CameraSizeMultiplier;
        [HideInAuthoring]
        public int RenderLayer;
    }

    [UseDefaultInspector(true)]
    public class PreviewRenderingSettingsComponent : ComponentDataBehaviour<PreviewRenderingSettings>
    {
        public Transform ItemPivot;

        protected override void Bake<K>(ref PreviewRenderingSettings serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.ItemPivot = baker.GetEntity(ItemPivot);
            serializedData.RenderLayer = gameObject.layer;
        }
    }
    
    #if UNITY_EDITOR
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TzarGames.Rendering.Baking.RendererBakingSystem))]
    public partial struct PreviewRenderingBakingSystem : ISystem
    {
        private EntityQuery renderFilterSettingsQuery;
        private EntityQuery previewRenderSettingsQuery;
        private SharedComponentTypeHandle<RenderFilterSettings> filterTypeHandle;

        public void OnCreate(ref SystemState state)
        {
            renderFilterSettingsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadWrite<TzarGames.Rendering.RenderFilterSettings>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            previewRenderSettingsQuery = state.GetEntityQuery(ComponentType.ReadOnly<PreviewRenderingSettings>());
            
            state.RequireForUpdate<PreviewRenderingSettings>();
            filterTypeHandle = state.GetSharedComponentTypeHandle<TzarGames.Rendering.RenderFilterSettings>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var settings = previewRenderSettingsQuery.GetSingleton<PreviewRenderingSettings>();
            
            updateRenderFilterSettings(settings, ref state);
        }
        
        void updateRenderFilterSettings(PreviewRenderingSettings settings, ref SystemState state)
        {
            var filterChunks = renderFilterSettingsQuery.ToArchetypeChunkArray(Allocator.Temp);
            filterTypeHandle.Update(ref state);

            foreach (var filterChunk in filterChunks)
            {
                var filterSettings = filterChunk.GetSharedComponent(filterTypeHandle);
                filterSettings.Layer = settings.RenderLayer;
                state.EntityManager.SetSharedComponent(filterChunk, filterSettings);
            }
        }
    }
    #endif
}
