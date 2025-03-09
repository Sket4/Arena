using System;
using MagicaCloth2;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Cloth
{
    [Serializable]
    public struct MagicaClothColliders : IBufferElementData
    {
        public Entity Collider;
    }

    public struct MagicaClothData : IComponentData
    {
        public Entity TargetRenderer;
    }

    public struct MagicaClothInitTag : IComponentData
    {
    }
    
    public class MagicaClothBaker : Baker<MagicaCloth>
    {
        public override void Bake(MagicaCloth authoring)
        {
            if (authoring.SerializeData.sourceRenderers.Count == 0)
            {
                return;
            }
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent<MagicaClothInitTag>(entity);
            
            AddComponentObject(entity, authoring);
            
            AddComponent(entity, new MagicaClothData
            {
                TargetRenderer = GetEntity(authoring.SerializeData.sourceRenderers[0], TransformUsageFlags.None)
            });

            var mr = GetComponent<MeshRenderer>();
            if (mr)
            {
                AddComponentObject(mr);
                var mf = GetComponent<MeshFilter>();
                if (mf)
                {
                    AddComponentObject(mf);    
                }
            }

             //var smr = GetComponent<SkinnedMeshRenderer>();
            // if (smr)
            // {
            //     AddComponentObject(smr);
            // }

            if (authoring.SerializeData.colliderCollisionConstraint.colliderList.Count > 0)
            {
                var colliders = AddBuffer<MagicaClothColliders>();
                
                foreach (var collider in authoring.SerializeData.colliderCollisionConstraint.colliderList)
                {
                    colliders.Add(new MagicaClothColliders
                    {
                        Collider = GetEntity(collider)
                    });
                }    
            }
            
        }
    }

    public class MagicaCapsuleColliderBaker : Baker<MagicaCapsuleCollider>
    {
        public override void Bake(MagicaCapsuleCollider authoring)
        {
            AddComponentObject(authoring);
            AddComponentObject(GetComponent<MeshRenderer>());
        }
    }
    public class MagicaSphereColliderBaker : Baker<MagicaSphereCollider>
    {
        public override void Bake(MagicaSphereCollider authoring)
        {
            AddComponentObject(authoring);
            AddComponentObject(GetComponent<MeshRenderer>());
        }
    }
}

