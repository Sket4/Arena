using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client.PreviewRendering
{
    public struct CreatePreviewItemRequest : IComponentData
    {
        public int PrefabID;
    }

    public struct PreviewItemInstance : IComponentData
    {
        public Entity Entity;
    }
    
    [BurstCompile]
    [DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct PreviewRenderingSystem : ISystem
    {
        private EntityQuery previewSettingsQuery;
        private EntityQuery databaseQuery;
        private EntityQuery requestsQuery;
        private EntityQuery previewCameraQuery;
        private EntityQuery renderFilterSettingsQuery;

        public void OnCreate(ref SystemState state)
        {
            previewSettingsQuery = state.GetEntityQuery(ComponentType.ReadOnly<PreviewRenderingSettings>());
            databaseQuery = state.GetEntityQuery(ComponentType.ReadOnly<MainDatabaseTag>(), ComponentType.ReadOnly<IdToEntity>());
            requestsQuery = state.GetEntityQuery(ComponentType.ReadOnly<CreatePreviewItemRequest>());
            previewCameraQuery = state.GetEntityQuery(ComponentType.ReadOnly<PreviewCamera>());
            state.RequireForUpdate<PreviewRenderingSettings>();
            state.RequireForUpdate<IdToEntity>();
            
            renderFilterSettingsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadWrite<RenderFilterSettings>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
        }

        void updateRenderFilterSettings(PreviewRenderingSettings settings, ref SystemState state)
        {
            var filterChunks = renderFilterSettingsQuery.ToArchetypeChunkArray(Allocator.Temp);
            var filterTypeHandle = state.GetSharedComponentTypeHandle<RenderFilterSettings>();

            foreach (var filterChunk in filterChunks)
            {
                var filterSettings = filterChunk.GetSharedComponent(filterTypeHandle);
                filterSettings.Layer = settings.RenderLayer;
                state.EntityManager.SetSharedComponent(filterChunk, filterSettings);
            }
        }

        void processCreateRequests(EntityCommandBuffer commands, PreviewRenderingSettings settings, ref SystemState state)
        {
            if (requestsQuery.IsEmpty)
            {
                return;
            }
            commands.DestroyEntity(requestsQuery);
            
            var requests = requestsQuery.ToComponentDataArray<CreatePreviewItemRequest>(Allocator.Temp);

            if (requests.Length > 1)
            {
                Debug.LogWarning("More than one create preview item requests");
            }

            var request = requests[requests.Length-1];
            
            var database = databaseQuery.GetSingletonBuffer<IdToEntity>();
                
            if (IdToEntity.TryGetEntityById(database, request.PrefabID, out var prefab) == false)
            {
                Debug.LogError($"Failed to find prefab with id {request.PrefabID}");
                return;
            }

            var em = state.EntityManager;
            
            if (em.HasComponent<ActivatedItemAppearance>(prefab) == false)
            {
                Debug.LogError($"no activated ite appearance on prefab {request.PrefabID}");
                return;
            }
            var appearance = em.GetComponentData<ActivatedItemAppearance>(prefab);
            var instance = commands.Instantiate(appearance.Prefab);
            
            commands.AddComponent(instance, new Parent
            {
                Value = settings.ItemPivot
            });
            commands.SetComponent(instance, LocalTransform.Identity);

            if (em.HasComponent<PreviewItemInstance>(settings.ItemPivot))
            {
                var instanceData = em.GetComponentData<PreviewItemInstance>(settings.ItemPivot);
                if (instanceData.Entity != Entity.Null)
                {
                    commands.DestroyEntity(instanceData.Entity);
                }

                instanceData.Entity = instance;
                commands.SetComponent(settings.ItemPivot, instanceData);
            }
            else
            {
                commands.AddComponent(settings.ItemPivot, new PreviewItemInstance
                {
                    Entity = instance
                });
            }
        }

        void updateCamera(in PreviewRenderingSettings settings, ref SystemState state)
        {
            var em = state.EntityManager;

            if (em.HasComponent<PreviewItemInstance>(settings.ItemPivot) == false)
            {
                return;
            }

            var previewInstance = em.GetComponentData<PreviewItemInstance>(settings.ItemPivot).Entity;
                
            if (em.HasBuffer<LinkedEntityGroup>(previewInstance) == false)
            {
                return;
            }
            
            var linkeds = em.GetBuffer<LinkedEntityGroup>(previewInstance);

            var bounds = new Bounds();
            
            foreach (var linked in linkeds)
            {
                if (em.HasComponent<WorldRenderBounds>(linked.Value))
                {
                    var worldBounds = em.GetComponentData<WorldRenderBounds>(linked.Value);
                    bounds.Encapsulate(new Bounds(worldBounds.Value.Center, worldBounds.Value.Size));
                }
            }

            if (bounds.size.sqrMagnitude < 0.01f)
            {
                Debug.LogError("bounds size is zero");
                return;
            }
            
            var cameraEntity = previewCameraQuery.GetSingletonEntity();
            em.SetComponentData(cameraEntity, new PreviewCamera
            {
                Center = bounds.center,
                OrthoSize = bounds.extents.y,
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var settings = previewSettingsQuery.GetSingleton<PreviewRenderingSettings>();
            
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                processCreateRequests(ecb, settings, ref state);
                updateCamera(settings, ref state);
                updateRenderFilterSettings(settings, ref state);
                
                ecb.Playback(state.EntityManager);
            }
        }
    }

    [UpdateAfter(typeof(PreviewRenderingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [DisableAutoCreation]
    public partial class PreviewRenderingManagedSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PreviewRenderingSettings>();
        }

        protected override void OnUpdate()
        {
            var settings = SystemAPI.GetSingleton<PreviewRenderingSettings>();
            
            Entities
                .WithoutBurst()
                .ForEach((Camera camera, Transform transform, PreviewCamera cameraData) =>
                {
                    var position = cameraData.Center + Vector3.forward * 3;
                    var rotation = quaternion.LookRotation(cameraData.Center - position, math.up());

                    camera.orthographicSize = cameraData.OrthoSize * settings.CameraSizeMultiplier;
                    transform.position = position;
                    transform.rotation = rotation;

                }).Run();
        }
    }
}
