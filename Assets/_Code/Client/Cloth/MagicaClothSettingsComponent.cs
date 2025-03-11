using System;
using MagicaCloth2;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Cloth
{
    [Serializable]
    public struct MagicaClothRenderCopies : IBufferElementData
    {
        public Entity Copy;
    }
    
    [Serializable]
    public struct MagicaClothSphereColliders : IBufferElementData
    {
        public Entity Collider;
    }
    [Serializable]
    public struct MagicaClothCapsuleColliders : IBufferElementData
    {
        public Entity Collider;
    }

    [UseDefaultInspector]
    public class MagicaClothSettingsComponent : DynamicBufferBehaviour<MagicaClothRenderCopies>
    {
        public Renderer[] Copies;
        public MagicaSphereCollider[] SphereColliders;
        public MagicaCapsuleCollider[] CapsuleColliders;

        protected override void Bake<K>(ref DynamicBuffer<MagicaClothRenderCopies> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if (SphereColliders != null && SphereColliders.Length > 0)
            {
                var colliders = baker.AddBuffer<MagicaClothSphereColliders>();
                foreach (var sphereCollider in SphereColliders)
                {
                    colliders.Add(new MagicaClothSphereColliders
                    {
                        Collider = baker.GetEntity(sphereCollider)
                    });
                }
            }
            if (CapsuleColliders != null && CapsuleColliders.Length > 0)
            {
                var colliders = baker.AddBuffer<MagicaClothCapsuleColliders>();
                foreach (var collider in CapsuleColliders)
                {
                    colliders.Add(new MagicaClothCapsuleColliders
                    {
                        Collider = baker.GetEntity(collider)
                    });
                }
            }

            foreach (var copy in Copies)
            {
                if (copy == false)
                {
                    continue;
                }
                serializedData.Add(new MagicaClothRenderCopies
                {
                    Copy = baker.GetEntity(copy, TransformUsageFlags.None)
                });
            }
        }
    }
}
