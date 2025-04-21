using System.Threading.Tasks;
using Arena.Client.MapWorks;
using Arena.ScriptViz;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Arena.Client
{
    sealed class RenderableNavMeshData : IComponentData
    {
        public Mesh Mesh;
    }

    [UpdateBefore(typeof(CommonEarlyGameSystem))]
    [DisableAutoCreation]
    public partial class ClientCommonGameplaySystem : GameSystemBase
    {
        private EntityQuery startQuestQuery;
        protected EntityQuery gameInterfaceQuery;
        private EntityQuery navMeshSettingsQuery;
        private EntityQuery mapCameraQuery;

        private RenderTexture mapRenderTexture;
        
        public struct QuestStartedFlag : IComponentData
        {
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            startQuestQuery = GetEntityQuery(ComponentType.ReadOnly<StartQuestRequest>());
            gameInterfaceQuery = GetEntityQuery(ComponentType.ReadOnly<GameInterface>());
            navMeshSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<ClientNavMeshSettings>());
            mapCameraQuery = GetEntityQuery(ComponentType.ReadWrite<Map>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Entities
                .WithoutBurst()
                .ForEach((RenderableNavMeshData data) =>
            {
                if (data.Mesh)
                {
                    Object.Destroy(data.Mesh);
                    data.Mesh = null;
                }
            }).Run();


            if (mapRenderTexture)
            {
                RenderTexture.ReleaseTemporary(mapRenderTexture);
                mapRenderTexture = null;
            }
        }

        protected override void OnSystemUpdate()
        {
            if (startQuestQuery.IsEmpty == false)
            {
                if (SystemAPI.HasSingleton<QuestStartedFlag>())
                {
                    Debug.LogError("quest already started");
                    EntityManager.DestroyEntity(startQuestQuery);
                    return;
                }
                
                var startRequests = startQuestQuery.ToComponentDataArray<StartQuestRequest>(Allocator.Temp);

                if (startRequests.Length > 1)
                {
                    Debug.LogWarning("Слишком много запросов на начало квеста, будет использован последний в списке");
                }
                var request = startRequests[startRequests.Length - 1];

                var mainDbEntity = SystemAPI.GetSingletonEntity<MainDatabaseTag>();
                var db = SystemAPI.GetBuffer<IdToEntity>(mainDbEntity);

                IdToEntity.TryGetEntityById(db, request.QuestID, out var questEntity);

                if (questEntity != Entity.Null)
                {
                    var gameInterface = gameInterfaceQuery.GetSingleton<GameInterface>();

                    int spawnPointId = 0;

                    if (SystemAPI.HasComponent<SpawnPointIdData>(questEntity))
                    {
                        spawnPointId = SystemAPI.GetComponent<SpawnPointIdData>(questEntity).ID;
                    }

                    EntityManager.CreateSingleton<QuestStartedFlag>(new FixedString64Bytes("quest started flag"));
                    GameParameter[] parameters = SystemAPI.HasBuffer<GameParameter>(questEntity)
                        ? SystemAPI.GetBuffer<GameParameter>(questEntity).AsNativeArray().ToArray()
                        : null;
				
                    _ = gameInterface.StartLocation(new QuestGameInfo
                    {
                        GameSceneID = SystemAPI.GetComponent<Quests.QuestData>(questEntity).GameSceneID,
                        SpawnPointID = spawnPointId,
                        MatchType = "ArenaMatch",
                        Multiplayer = false,
                        Parameters = parameters
                    });
                }
                else
                {
                    Debug.LogError($"Failed to find quest with id {request.QuestID} in database {mainDbEntity}");
                }
                
                EntityManager.DestroyEntity(startQuestQuery);
            }
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<NavMeshManagedData, MapBounds>()
                .WithNone<RenderableNavMeshData>()
                .ForEach((Entity entity, in NavMeshManagedData data) =>
                {
                    if (data.IsProcessed == false)
                    {
                        return;
                    }
                    createRenderableNavMesh(entity, true);
                }).Run();

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<GeneratedNavMeshData>()
                .WithNone<RenderableNavMeshData>()
                .ForEach((Entity entity) => { createRenderableNavMesh(entity, true); }).Run();
            
            Entities
                .WithoutBurst()
                .WithChangeFilter<MapBounds>()
                .ForEach((in MapBounds bounds) =>
            {
                if (mapCameraQuery.TryGetSingleton(out Map mapCamera))
                {
                    mapCamera.SetBounds(bounds.Bounds);   
                }
            }).Run();
            
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in MapRender mapRenderData, in LocalToWorld l2w) =>
                {
                    renderMapCamera(mapRenderData, l2w);
                    EntityManager.DestroyEntity(entity);
                
            }).Run();
        }

        private async void createRenderableNavMesh(Entity entity, bool addMapBounds)
        {
            await Task.Yield();
            
            var renderableMesh = navMeshToMesh();
            var go = new GameObject("Renderable navmesh");
            go.layer = LayerMask.NameToLayer("Map");
            go.transform.position = new Vector3(0, -10, 0);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = renderableMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.receiveShadows = false;
            mr.allowOcclusionWhenDynamic = false;

            var navMeshSettings = navMeshSettingsQuery.GetSingleton<ClientNavMeshSettings>();
            mr.material = navMeshSettings.MapMaterial;

            EntityManager.AddComponentObject(entity, new RenderableNavMeshData
            {
                Mesh = renderableMesh
            });
            if (addMapBounds)
            {
                var bounds = new MapBounds
                {
                    Bounds = mr.bounds
                };
                
                if (EntityManager.HasComponent<MapBounds>(entity))
                {
                    EntityManager.SetComponentData(entity, bounds);        
                }
                else
                {
                    EntityManager.AddComponentData(entity, bounds);
                }
            }
        }

        async void renderMapCamera(MapRender mapRenderData, LocalToWorld l2w)
        {
            Debug.Log("Render map");
                    
            var renderInfo = EntityManager.GetSharedComponentManaged<RenderInfo>(mapRenderData.MapPlaneMesh);
            if (renderInfo.Material.LoadingStatus == ObjectLoadingStatus.None)
            {
                renderInfo.Material.LoadAsync();
            }

            while (renderInfo.Material.LoadingStatus != ObjectLoadingStatus.Completed && renderInfo.Material.LoadingStatus != ObjectLoadingStatus.Error)
            {
                await Task.Yield();
                if (World.IsCreated == false)
                {
                    return;
                }
            }
            
            if (renderInfo.Material.Result == false)
            {
                Debug.Log("Failed to load map plane mesh material");
                return;    
            }

            var mapTextureSize = mapRenderData.MapTextureSize;

            var useHiRes = SystemInfo.graphicsMemorySize > 512;
            var renderTextureSize = useHiRes ? mapTextureSize * 2 : mapTextureSize;
            var targetRenderTexture = RenderTexture.GetTemporary(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
            targetRenderTexture.name = "Map render texture hires";
            

            var rs = World.GetExistingSystemManaged<RenderingSystem>();

            await Task.Yield();

            var loadResult = await GameLoopUtils.WaitForResourcesLoad(World);
            if (loadResult == false)
            {
                return;
            }
            
            if (mapRenderData.MapPostprocessMaterial.LoadingStatus == ObjectLoadingStatus.None)
            {
                mapRenderData.MapPostprocessMaterial.LoadAsync();
            }

            while (mapRenderData.MapPostprocessMaterial.LoadingStatus != ObjectLoadingStatus.Completed 
                   && mapRenderData.MapPostprocessMaterial.LoadingStatus != ObjectLoadingStatus.Error)
            {
                await Task.Yield();
            }
            
            var cameraGO = new GameObject("map render camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.cullingMask = mapRenderData.MapRenderLayers;
            camera.farClipPlane = 200;
            camera.orthographicSize = mapRenderData.MapCameraOrthoSize;
            camera.orthographic = true;
            camera.targetTexture = targetRenderTexture;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = l2w.Position;
            camera.transform.rotation = l2w.Rotation;
            camera.transparencySortMode = TransparencySortMode.Orthographic;
            
            Shader.EnableKeyword("ARENA_MAP_RENDER");

            var lightsEnabled = DGX.SRP.RenderPipeline.IsLightsEnabled;
            DGX.SRP.RenderPipeline.EnableLights(false);
            camera.Render();
            DGX.SRP.RenderPipeline.EnableLights(lightsEnabled);
            
            Shader.DisableKeyword("ARENA_MAP_RENDER");
            
            if (mapRenderData.MapPostprocessMaterial.Result != null)
            {
                var temp = RenderTexture.GetTemporary(targetRenderTexture.descriptor);
                Graphics.Blit(targetRenderTexture, temp, mapRenderData.MapPostprocessMaterial.Result);
                RenderTexture.ReleaseTemporary(targetRenderTexture);
                targetRenderTexture = temp;
            }
            else
            {
                Debug.LogError("failed to load map postprocess material"); 
            }
            camera.enabled = false;

            if (mapRenderTexture)
            {
                Debug.LogWarning("Map render texture already exist");
                RenderTexture.ReleaseTemporary(mapRenderTexture);
                mapRenderTexture = null;
            }

            if (useHiRes)
            {
                mapRenderTexture = RenderTexture.GetTemporary(mapTextureSize, mapTextureSize, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
                Graphics.Blit(targetRenderTexture, mapRenderTexture);
                RenderTexture.ReleaseTemporary(targetRenderTexture);
            }
            else
            {
                mapRenderTexture = targetRenderTexture;
            }
            
            mapRenderTexture.name = "Map render texture";
            renderInfo.Material.Result.mainTexture = mapRenderTexture;

            var msgEntity = EntityManager.CreateEntity(typeof(Message));
            EntityManager.SetComponentData(msgEntity, Message.CreateFromString("map rendered"));
            Object.Destroy(cameraGO);
        }
        
        static Mesh navMeshToMesh()
        {
            NavMeshTriangulation triangulatedNavMesh = NavMesh.CalculateTriangulation();

            Mesh mesh = new Mesh();
            mesh.name = "ExportedNavMesh";
            mesh.vertices = triangulatedNavMesh.vertices;
            mesh.triangles = triangulatedNavMesh.indices;

            return mesh;
        }
    }
}
