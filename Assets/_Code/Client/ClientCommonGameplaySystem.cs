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
				
                    _ = gameInterface.StartQuest(new QuestGameInfo
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
                .WithAll<GeneratedNavMeshData>()
                .WithNone<RenderableNavMeshData>()
                .ForEach((Entity entity) =>
                {
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
                    EntityManager.AddComponentData(entity, new MapBounds
                    {
                        Bounds = mr.bounds
                    });

                }).Run();
            
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
                    Debug.Log("Render map");
                    
                    var renderInfo = EntityManager.GetSharedComponentManaged<RenderInfo>(mapRenderData.MapPlaneMesh);
                    if (renderInfo.Material.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        renderInfo.Material.LoadAsync();
                    }
                    renderInfo.Material.UnityReference.WaitForCompletion();

                    if (renderInfo.Material.Result)
                    {
                        var texture = RenderTexture.GetTemporary(mapRenderData.MapTextureSize, mapRenderData.MapTextureSize, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 8);
                        renderInfo.Material.Result.mainTexture = texture;

                        renderMapCamera(mapRenderData, texture, l2w);    
                    }
                    else
                    {
                        Debug.Log("Failed to load map plane mesh material");
                    }
                    
                    EntityManager.DestroyEntity(entity);
                
            }).Run();
        }

        async void renderMapCamera(MapRender mapRenderData, RenderTexture texture, LocalToWorld l2w)
        {
            await Task.Yield();
            await Task.Yield();

            var rs = World.GetExistingSystemManaged<RenderingSystem>();

            while (rs.LoadingMaterialCount > 0 || rs.LoadingMeshCount > 0)
            {
                await Task.Yield();
                
                if (World.IsCreated == false)
                {
                    return;
                }
            }
            
            var cameroGO = new GameObject("map render camera");
            var camera = cameroGO.AddComponent<Camera>();
            camera.cullingMask = mapRenderData.MapRenderLayers;
            camera.orthographicSize = mapRenderData.MapCameraOrthoSize;
            camera.orthographic = true;
            camera.targetTexture = texture;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = l2w.Position;
            camera.transform.rotation = l2w.Rotation;
                    
            camera.Render();

            camera.enabled = false;

            var msgEntity = EntityManager.CreateEntity(typeof(Message));
            EntityManager.SetComponentData(msgEntity, Message.CreateFromString("map rendered"));
            //Object.Destroy(cameroGO);
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
