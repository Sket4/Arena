using System.Collections.Generic;
using MagicaCloth2;
using TzarGames.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena.Client.Cloth
{
    partial class MagicaClothSystem : SystemBase
    {
        private EntityQuery initQuery;
        private List<MagicaCloth> changedRenderers = new();

        bool tryGetChildComponent<T>(Entity entity, out T component) 
            where T : UnityEngine.Component
        {
            if (EntityManager.HasComponent<T>(entity))
            {
                component = EntityManager.GetComponentObject<T>(entity);
                return true;
            }

            if (EntityManager.HasBuffer<LinkedEntityGroup>(entity))
            {
                var linkeds = EntityManager.GetBuffer<LinkedEntityGroup>(entity);
                foreach (var child in linkeds)
                {
                    if (child.Value == entity)
                    {
                        continue;
                    }
                    if (tryGetChildComponent<T>(child.Value, out component))
                    {
                        return true;
                    }
                } 
            }

            if (EntityManager.HasBuffer<Child>(entity))
            {
                var childs = EntityManager.GetBuffer<Child>(entity);
                foreach (var child in childs)
                {
                    if (tryGetChildComponent<T>(child.Value, out component))
                    {
                        return true;
                    }
                }
            }

            
            component = null;
            return false;
        }

        protected override void OnUpdate()
        {
            Entities
                .WithChangeFilter<CharacterEquipmentAppearanceState>()
                .WithoutBurst()
                .ForEach((in CharacterEquipmentAppearanceState state) =>
                {
                    if (tryGetChildComponent(state.HairModelEntity, out MagicaCloth hairCloth) == false)
                    {
                        return;
                    }

                    var headAppearance = EntityManager.GetComponentData<HeadAppearance>(state.HeadModelEntity);
                    var headCollider =
                        EntityManager.GetComponentObject<MagicaSphereCollider>(headAppearance.ClothHeadCollider);
                    var neckCollider =
                        EntityManager.GetComponentObject<MagicaCapsuleCollider>(headAppearance.ClothNeckCollider);
                    
                    
                    
                    hairCloth.SerializeData.colliderCollisionConstraint.colliderList.Clear();
                    if (headCollider)
                    {
                        hairCloth.SerializeData.colliderCollisionConstraint.colliderList.Add(headCollider);
                    }

                    if (neckCollider)
                    {
                        hairCloth.SerializeData.colliderCollisionConstraint.colliderList.Add(neckCollider);
                    }

                    if (EntityManager.HasComponent<CharacterAppearanceState>(state.ArmorSetEntity))
                    {
                        var armorState = EntityManager.GetComponentData<CharacterAppearanceState>(state.ArmorSetEntity);
                        if (EntityManager.HasBuffer<MagicaClothCapsuleColliders>(armorState.Instance))
                        {
                            var colliders = EntityManager.GetBuffer<MagicaClothCapsuleColliders>(armorState.Instance);
                            foreach (var collider in colliders)
                            {
                                var caps = EntityManager.GetComponentObject<MagicaCapsuleCollider>(collider.Collider);
                                hairCloth.SerializeData.colliderCollisionConstraint.colliderList.Add(caps);
                            }
                        }
                        if (EntityManager.HasBuffer<MagicaClothSphereColliders>(armorState.Instance))
                        {
                            var colliders = EntityManager.GetBuffer<MagicaClothSphereColliders>(armorState.Instance);
                            foreach (var collider in colliders)
                            {
                                var caps = EntityManager.GetComponentObject<MagicaSphereCollider>(collider.Collider);
                                hairCloth.SerializeData.colliderCollisionConstraint.colliderList.Add(caps);
                            }
                        }
                    }

                }).Run();
            
            if (changedRenderers.Count == 0 && initQuery.IsEmpty)
            {
                return;
            }
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref initQuery)
                .WithAll<MagicaClothInitTag>()
                .ForEach((Entity entity, MagicaCloth cloth) =>
                {
                    if (EntityManager.HasComponent<MeshRenderer>(entity))
                    {
                        var mr = EntityManager.GetComponentObject<MeshRenderer>(entity);
                        mr.enabled = false;    
                    }

                    if (EntityManager.HasComponent<SkinnedMeshRenderer>(entity))
                    {
                        var smr = EntityManager.GetComponentObject<SkinnedMeshRenderer>(entity);
                        smr.enabled = false;
                    }
                    
                    ecb.RemoveComponent<MagicaClothInitTag>(entity);
                    cloth.OnRendererMeshChange += OnRendererMeshChange;

                    if (EntityManager.HasComponent<MagicaClothColliders>(entity))
                    {
                        var list = cloth.SerializeData.colliderCollisionConstraint.colliderList;
                        var colliders = EntityManager.GetBuffer<MagicaClothColliders>(entity);
                    
                        foreach (var collider in colliders)
                        {
                            if (EntityManager.HasComponent<MagicaCapsuleCollider>(collider.Collider))
                            {
                                var capsule = EntityManager.GetComponentObject<MagicaCapsuleCollider>(collider.Collider);

                                if (capsule)
                                {
                                    if (list.Contains(capsule) == false)
                                    {
                                        list.Add(capsule);
                                    }    
                                }    
                            }

                            if (EntityManager.HasComponent<MagicaSphereCollider>(collider.Collider))
                            {
                                var sphere = EntityManager.GetComponentObject<MagicaSphereCollider>(collider.Collider);

                                if (sphere)
                                {
                                    if (list.Contains(sphere) == false)
                                    {
                                        list.Add(sphere);
                                    }    
                                }    
                            }
                        }
                    }

                }).Run();

            if (changedRenderers.Count > 0)
            {
                Entities
                    .WithoutBurst()
                    .ForEach((Entity entity, MagicaCloth cloth, in MagicaClothData data) =>
                    {
                        if (changedRenderers.Contains(cloth) == false)
                        {
                            return;
                        }

                        Renderer renderer = null;
                        
                        if (EntityManager.HasComponent<MeshRenderer>(entity))
                        {
                            renderer = EntityManager.GetComponentObject<MeshRenderer>(entity);
                        }
                        else if (EntityManager.HasComponent<SkinnedMeshRenderer>(entity))
                        {
                            renderer = EntityManager.GetComponentObject<SkinnedMeshRenderer>(entity);
                        }
                        
                        var customMesh = cloth.GetCustomMesh(renderer);
                        var newRef = CustomWeakObjectReference<Mesh>.CreateWithCustomModeLoading(customMesh);

                        if (EntityManager.HasComponent<RenderInfo>(entity))
                        {
                            setupRenderer(entity, newRef, ecb);
                        }
                        
                        if (EntityManager.HasBuffer<MagicaClothRenderCopies>(entity))
                        {
                            var renderCopies = EntityManager.GetBuffer<MagicaClothRenderCopies>(entity).ToNativeArray(Allocator.Temp);

                            foreach (var renderCopy in renderCopies)
                            {
                                if (EntityManager.HasComponent<RenderInfo>(renderCopy.Copy))
                                {
                                    setupRenderer(renderCopy.Copy, newRef, ecb);
                                }
                                
                                if (EntityManager.HasComponent<CPUSkinnedMeshInstance>(renderCopy.Copy))
                                {
                                    var cpuInstance =
                                        EntityManager.GetComponentObject<CPUSkinnedMeshInstance>(renderCopy.Copy);
                                    cpuInstance.MeshInstance = customMesh;        
                                }
                            }
                        }

                    }).Run();
                
                changedRenderers.Clear();
            }

            if (ecb.ShouldPlayback)
            {
                ecb.Playback(EntityManager);
            }
        }

        void setupRenderer(Entity target, CustomWeakObjectReference<Mesh> newRef, EntityCommandBuffer ecb)
        {
            var renderInfo = EntityManager.GetSharedComponentManaged<RenderInfo>(target);
            var meshInfo = EntityManager.GetComponentData<MeshInfo>(target);
            meshInfo.MeshID = BatchMeshID.Null;
            
            meshInfo.MeshIndex = renderInfo.AddMeshReference(newRef);
            renderInfo.ResetHash128_Managed();
            ecb.SetSharedComponentManaged(target, renderInfo);
            ecb.SetComponent(target, meshInfo);
        }

        private void OnRendererMeshChange(MagicaCloth arg1, Renderer arg2, bool arg3)
        {
            Debug.Log($"Renderer mesh changed {arg2.name} tp {arg3}");
            changedRenderers.Add(arg1);
        }
    }
}