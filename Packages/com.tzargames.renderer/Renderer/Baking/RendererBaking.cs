using System.Collections.Generic;
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace TzarGames.Renderer.Baking
{
    #if UNITY_EDITOR
    public class SubMeshBakingData
    {
        public Entity Entity;
        public Material Material;
        public byte SubMeshIndex;
    }
    
    [TemporaryBakingType]
    public class RendererBakingData : IComponentData
    {
        public UnityEngine.Renderer Authoring;
        public AABB Bounds;
        public Mesh Mesh;
        public List<SubMeshBakingData> SubMeshBakingDatas;
    }
    
    public class SkinnedMeshRendererBaker : Baker<SkinnedMeshRenderer>
    {
        public override void Bake(SkinnedMeshRenderer authoring)
        {
            var skinMatrix = AddBuffer<SkinMatrix>();
            skinMatrix.ResizeUninitialized(authoring.bones.Length);
            
            DependsOn(authoring.sharedMesh);
            DependsOn(authoring.sharedMaterial);
            
            var bakingData = new RendererBakingData
            {
                Authoring = authoring,
                Bounds = authoring.bounds.ToAABB(),
                Mesh = authoring.sharedMesh,
                SubMeshBakingDatas = new List<SubMeshBakingData>()
            };
            bakingData.SubMeshBakingDatas.Add(new SubMeshBakingData()
            {
                SubMeshIndex = 0,
                Entity = GetEntity(TransformUsageFlags.Renderable),
                Material = authoring.sharedMaterial
            });
            
            AddComponentObject(bakingData);
        }
    }

    public class MeshRendererBaker : Baker<MeshRenderer>
    {
        public override void Bake(MeshRenderer authoring)
        {
            var mf = GetComponent<MeshFilter>();
            var mesh = mf.sharedMesh;

            if(mesh == null)
            {
                Debug.LogError("Null mesh on " + authoring.name);
                return;
            }
            
            DependsOn(mf);
            DependsOn(mesh);

            // process submeshes
            var sharedMats = authoring.sharedMaterials;

            var matsCount = sharedMats.Length;
            var subMeshCount = mesh.subMeshCount;
                
            if (matsCount < subMeshCount)
            {
                Debug.LogWarning($"Material count ({matsCount}) less than sub mesh count ({subMeshCount}) on object: {authoring.name}");
            }

            int min = math.min(matsCount, subMeshCount);
            
            if (min > 0)
            {
                var bakingData = new RendererBakingData
                {
                    Authoring = authoring,
                    Bounds = authoring.localBounds.ToAABB(),
                    Mesh = mesh,
                    SubMeshBakingDatas = new List<SubMeshBakingData>()
                };

                var thisEntity = GetEntity(TransformUsageFlags.Renderable);
                
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    var mat = sharedMats[i];
                    Entity renderableEntity;

                    if (i == 0)
                    {
                        renderableEntity = thisEntity;
                    }
                    else
                    {
                        renderableEntity = CreateAdditionalEntity(TransformUsageFlags.Renderable, entityName: $"{authoring.name} submesh {i}");
                    }

                    var subMeshIndex = math.min(i, subMeshCount - 1);
                    
                    bakingData.SubMeshBakingDatas.Add(new SubMeshBakingData
                    {
                        SubMeshIndex = (byte)subMeshIndex,
                        Material = mat,
                        Entity = renderableEntity
                    });
                }
                
                AddComponentObject(thisEntity, bakingData);
            }
        }
    }

#if UNITY_EDITOR
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class RendererBakingSystem : SystemBase
    {
        static readonly uint4 InvalidId = new uint4(0, 0, 14, 0);

        static bool IsInvalidReference(in UntypedWeakReferenceId id)
        {
            return id.GlobalId.AssetGUID.Value.Equals(InvalidId);
        }

        static bool IsInvalidReference(Object obj)
        {
            return IsInvalidReference(UntypedWeakReferenceId.CreateFromObjectInstance(obj));
        }

        protected override void OnUpdate()
        {
            var renderMeshDict = new Dictionary<Material, List<Mesh>>();

            // collect mats and meshes
            Entities
                .WithoutBurst()
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((RendererBakingData bakingData) =>
            {
                foreach (var data in bakingData.SubMeshBakingDatas)
                {
                    if (data.Material == null)
                    {
                        Debug.LogWarning($"Null material on {bakingData.Authoring.name}");
                        continue;
                    }
                    
                    List<Mesh> meshes;
                    var prefabParent = UnityEditor.PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(bakingData.Authoring.gameObject);

                    if (renderMeshDict.TryGetValue(data.Material, out meshes) == false)
                    {
                        meshes = new List<Mesh>();
                        renderMeshDict.Add(data.Material, meshes);

                        if(IsInvalidReference(data.Material))
                        {
                            Debug.LogError($"Invalid reference for material {data.Material} on object {bakingData.Authoring} ({prefabParent})");
                        }
                    }

                    if (meshes.Contains(bakingData.Mesh) == false)
                    {
                        meshes.Add(bakingData.Mesh);

                        if(IsInvalidReference(bakingData.Mesh))
                        {
                            Debug.LogError($"Invalid reference for mesh {bakingData.Mesh} on object {bakingData.Authoring} ({prefabParent})");
                        }
                    }
                }
            }).Run();

            if (renderMeshDict.Count == 0)
            {
                return;
            }

            var renderInfoDict = new Dictionary<Material, RenderInfo>();

            var tempRefHolder = SystemAPI.GetSingletonEntity<TempAssetReferenceElement>();

            // TODO убрать это, когда исправится баг с сериализацией RenderInfo
            if(tempRefHolder != Entity.Null)
            {
                Debug.Log("WRITING TEMP MESH AND MAT HOLDERS");

                var matList = new List<Material>();
                var meshList = new List<Mesh>();

                foreach(var kv in renderMeshDict)
                {
                    if(kv.Key != null)
                    {
                        if (matList.Contains(kv.Key) == false)
                        {
                            matList.Add(kv.Key);
                        }
                    }
                    else
                    {
                        Debug.LogError("null mat");
                    }

                    foreach(var mesh in kv.Value)
                    {
                        if(mesh == null)
                        {
                            Debug.Log("null mesh");
                            continue;
                        }
                        if(meshList.Contains(mesh) == false)
                        {
                            meshList.Add(mesh);
                        }
                    }
                }

                // для проверки встроенных ассетов
                

                var refs = SystemAPI.GetBuffer<TempAssetReferenceElement>(tempRefHolder);
                refs.Clear();

                foreach(var m in matList)
                {
                    refs.Add(new TempAssetReferenceElement
                    {
                        Value = UntypedWeakReferenceId.CreateFromObjectInstance(m)
                    });
                }

                foreach(var m in meshList)
                {
                    refs.Add(new TempAssetReferenceElement 
                    {
                        Value = UntypedWeakReferenceId.CreateFromObjectInstance(m) 
                    });
                }
            }
            else
            {
                Debug.LogError("NO TEMP REF HOLDER");
            }

            foreach (var kv in renderMeshDict)
            {
                var meshArray = new WeakObjectReference<Mesh>[kv.Value.Count];

                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var mesh = kv.Value[i];
                    meshArray[i] = new WeakObjectReference<Mesh>(mesh);
                }

                var ri = new RenderInfo
                {
                    Material = new WeakObjectReference<Material>(kv.Key),
                    MeshArray = meshArray
                };
                ri.ResetHash128();
                
                renderInfoDict.Add(kv.Key, ri);
            }
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .WithoutBurst()
                .ForEach((RendererBakingData bakingData) =>
                {
                    foreach (var data in bakingData.SubMeshBakingDatas)
                    {
                        if (data.Material == null)
                        {
                            continue;
                        }
                        
                        var renderInfo = renderInfoDict[data.Material];
                        var meshList = renderMeshDict[data.Material];
                        
                        ecb.AddSharedComponentManaged(data.Entity, renderInfo);
                        ecb.AddComponent(data.Entity, new MeshInfo
                        {
                            MeshID = BatchMeshID.Null,
                            SubMeshIndex = data.SubMeshIndex,
                            MeshIndex = meshList.IndexOf(bakingData.Mesh)
                        });
                        ecb.AddComponent<MeshInfoLoadingStatus>(data.Entity);
                        ecb.AddComponent(data.Entity, new LocalRenderBounds { Value = bakingData.Bounds });
                        ecb.AddComponent(data.Entity, new WorldRenderBounds { Value = bakingData.Bounds });
                    }
                }).Run();
            
            ecb.Playback(EntityManager);
        }
    }
#endif
#endif
}
