using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client.PreviewRendering
{
    public struct CreatePreviewItemRequest : IComponentData
    {
        public int PrefabID;
        public PackedColor Color;
    }

    public struct ChangeColorRequest : IComponentData
    {
        public PackedColor Color;
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
        private EntityQuery createItemRequestsQuery;
        private EntityQuery changeColorRequestsQuery;
        private EntityQuery previewCameraQuery;
        private EntityQuery inventoryQuery;
        private Aabb characterBounds;
        private bool isCharacterBoundsCalculated;

        public void OnCreate(ref SystemState state)
        {
            previewSettingsQuery = state.GetEntityQuery(ComponentType.ReadOnly<PreviewRenderingSettings>());
            databaseQuery = state.GetEntityQuery(ComponentType.ReadOnly<MainDatabaseTag>(), ComponentType.ReadOnly<IdToEntity>());
            createItemRequestsQuery = state.GetEntityQuery(ComponentType.ReadOnly<CreatePreviewItemRequest>());
            changeColorRequestsQuery = state.GetEntityQuery(ComponentType.ReadOnly<ChangeColorRequest>());
            previewCameraQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new [] { ComponentType.ReadOnly<PreviewCamera>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
            });
            inventoryQuery = state.GetEntityQuery(ComponentType.ReadOnly<InventoryElement>());
            
            state.RequireForUpdate<PreviewRenderingSettings>();
            state.RequireForUpdate<IdToEntity>();
        }
        
        void createArmorSet(EntityManager em, Entity armorSetPrefab, PackedColor color, EntityCommandBuffer commands)
        {
            var prefabId = em.GetComponentData<Item>(armorSetPrefab);
            var inventoryEntity = inventoryQuery.GetSingletonEntity();
            var inventory = em.GetBuffer<InventoryElement>(inventoryEntity);

            var existingItem = Entity.Null;
            
            foreach (var element in inventory)
            {
                var item = em.GetComponentData<Item>(element.Entity);
                if (item.ID == prefabId.ID)
                {
                    existingItem = element.Entity;
                }
                else
                {
                    if (em.HasComponent<ArmorSet>(element.Entity))
                    {
                        var state = em.GetComponentData<ActivatedState>(element.Entity);
                        if (state.Activated)
                        {
                            commands.SetComponent(element.Entity, new ActivatedState(false));    
                        }
                    }    
                }
            }

            if (existingItem != Entity.Null)
            {
                commands.SetComponent(existingItem, new ActivatedState(true));   
            }
            else
            {
                var instance = commands.Instantiate(armorSetPrefab);
                commands.SetComponent(instance, new ActivatedState(true));

                if (em.HasComponent<SyncedColor>(armorSetPrefab))
                {
                    commands.SetComponent(instance, new SyncedColor { Value = color });
                }
                
                var transactionEntity = commands.CreateEntity();
                commands.AddComponent<InventoryTransaction>(transactionEntity);
                commands.AddComponent<EventTag>(transactionEntity);
                commands.AddComponent(transactionEntity, new Target(inventoryEntity));
                var itemsToAdd = commands.AddBuffer<ItemsToAdd>(transactionEntity);
                itemsToAdd.Add(new ItemsToAdd(instance));
            }
        }

        void processChangeColorRequests(EntityCommandBuffer commands, PreviewRenderingSettings settings, ref SystemState state)
        {
            if (changeColorRequestsQuery.IsEmpty)
            {
                return;
            }
            commands.DestroyEntity(changeColorRequestsQuery);
            
            var requests = changeColorRequestsQuery.ToComponentDataArray<ChangeColorRequest>(Allocator.Temp);

            if (requests.Length > 1)
            {
                Debug.LogWarning("More than one change color requests");
            }
            var request = requests[requests.Length-1];

            var em = state.EntityManager;

            if (inventoryQuery.TryGetSingletonBuffer<InventoryElement>(out var inventory, true))
            {
                foreach (var item in inventory)
                {
                    if (em.HasComponent<SyncedColor>(item.Entity))
                    {
                        commands.SetComponent(item.Entity, new SyncedColor { Value = request.Color });
                    }
                }
            }
        }

        bool processCreateRequests(EntityCommandBuffer commands, PreviewRenderingSettings settings, ref SystemState state)
        {
            if (createItemRequestsQuery.IsEmpty)
            {
                return false;
            }
            commands.DestroyEntity(createItemRequestsQuery);
            
            var requests = createItemRequestsQuery.ToComponentDataArray<CreatePreviewItemRequest>(Allocator.Temp);

            if (requests.Length > 1)
            {
                Debug.LogWarning("More than one create preview item requests");
            }

            var request = requests[requests.Length-1];
            
            var database = databaseQuery.GetSingletonBuffer<IdToEntity>();
                
            if (IdToEntity.TryGetEntityById(database, request.PrefabID, out var prefab) == false)
            {
                Debug.LogError($"Failed to find prefab with id {request.PrefabID}");
                return false;
            }

            var em = state.EntityManager;

            var hasPreviewInstance = em.HasComponent<PreviewItemInstance>(settings.ItemPivot);

            if (hasPreviewInstance)
            {
                var instanceData = em.GetComponentData<PreviewItemInstance>(settings.ItemPivot);
                
                if (instanceData.Entity != Entity.Null)
                {
                    commands.DestroyEntity(instanceData.Entity);
                }
            }

            if (em.HasComponent<ArmorSet>(prefab))
            {
                commands.SetComponent(settings.ItemPivot, new PreviewItemInstance
                {
                    Entity = Entity.Null
                });
                createArmorSet(em, prefab, request.Color, commands);
                return true;
            }

            if (inventoryQuery.HasSingleton<InventoryElement>())
            {
                var inventoryEntity = inventoryQuery.GetSingletonEntity();
                var inventory = em.GetBuffer<InventoryElement>(inventoryEntity);

                foreach (var inventoryElement in inventory)
                {
                    if (em.HasComponent<ActivatedState>(inventoryElement.Entity))
                    {
                        commands.SetComponent(inventoryElement.Entity, new ActivatedState(false));
                    }
                }
            }
            
            if (em.HasComponent<ActivatedItemAppearance>(prefab) == false)
            {
                Debug.LogError($"no activated ite appearance on prefab {request.PrefabID}");
                return false;
            }
            var appearance = em.GetComponentData<ActivatedItemAppearance>(prefab);
            var instance = commands.Instantiate(appearance.Prefab);
            
            commands.AddComponent(instance, new Parent
            {
                Value = settings.ItemPivot
            });
            commands.SetComponent(instance, LocalTransform.Identity);

            if(hasPreviewInstance)
            {
                commands.SetComponent(settings.ItemPivot, new PreviewItemInstance
                {
                    Entity = instance
                });
            }
            else
            {
                commands.AddComponent(settings.ItemPivot, new PreviewItemInstance
                {
                    Entity = instance
                });
            }
            
            return true;
        }

        void updateCamera(in PreviewRenderingSettings settings, ref SystemState state)
        {
            var em = state.EntityManager;
            
            var cameraEntity = previewCameraQuery.GetSingletonEntity();

            if (em.HasComponent<Disabled>(cameraEntity))
            {
                return;
            }

            if (em.HasComponent<PreviewItemInstance>(settings.ItemPivot) == false)
            {
                return;
            }

            var previewInstance = em.GetComponentData<PreviewItemInstance>(settings.ItemPivot).Entity;

            if (previewInstance == Entity.Null)
            {
                if (isCharacterBoundsCalculated == false)
                {
                    if (inventoryQuery.HasSingleton<InventoryElement>())
                    {
                        var characterEntity = inventoryQuery.GetSingletonEntity();
                        var collider = em.GetComponentData<PhysicsCollider>(characterEntity);
                        var position = em.GetComponentData<LocalToWorld>(characterEntity).Position;
                        characterBounds = collider.Value.Value.CalculateAabb(new RigidTransform(quaternion.identity, position));
                        isCharacterBoundsCalculated = true;
                    }
                }

                if (isCharacterBoundsCalculated)
                {
                    var camera = previewCameraQuery.GetSingletonEntity();
                    em.SetComponentData(camera, new PreviewCamera
                    {
                        Center = characterBounds.Center,
                        OrthoSize = characterBounds.Extents.y * 0.5f,
                    });    
                }
                return;
            }
                
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
            
            em.SetComponentData(cameraEntity, new PreviewCamera
            {
                Center = bounds.center,
                OrthoSize = bounds.extents.y,
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var settings = previewSettingsQuery.GetSingleton<PreviewRenderingSettings>();

            setCameraState(true, ref state);
            
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                if (processCreateRequests(ecb, settings, ref state))
                {
                    setCameraState(false, ref state);
                }
                processChangeColorRequests(ecb, settings, ref state);
                updateCamera(settings, ref state);
                
                ecb.Playback(state.EntityManager);
            }
        }

        void setCameraState(bool enabled, ref SystemState state)
        {
            if (previewCameraQuery.TryGetSingletonEntity<PreviewCamera>(out var entity))
            {
                if (enabled)
                {
                    if (state.EntityManager.HasComponent<Disabled>(entity))
                    {
                        state.EntityManager.RemoveComponent<Disabled>(entity);
                    }
                }
                else
                {
                    if (state.EntityManager.HasComponent<Disabled>(entity) == false)
                    {
                        state.EntityManager.AddComponent<Disabled>(entity);
                    }
                }
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
                .WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities)
                .WithoutBurst()
                .ForEach((Entity entity, Camera camera, Transform transform, PreviewCamera cameraData) =>
                {
                    if (EntityManager.HasComponent<Disabled>(entity))
                    {
                        camera.enabled = false;
                        return;
                    }
                    camera.enabled = true;
                    
                    var position = cameraData.Center + Vector3.forward * 3;
                    var rotation = quaternion.LookRotation(cameraData.Center - position, math.up());

                    camera.orthographicSize = cameraData.OrthoSize * settings.CameraSizeMultiplier;
                    transform.position = position;
                    transform.rotation = rotation;

                }).Run();
        }
    }
}
