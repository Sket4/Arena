using Arena.Quests;
using Arena.ScriptViz;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
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
