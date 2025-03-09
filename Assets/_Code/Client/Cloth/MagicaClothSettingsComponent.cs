using System;
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

    [UseDefaultInspector]
    public class MagicaClothSettingsComponent : DynamicBufferBehaviour<MagicaClothRenderCopies>
    {
        public Renderer[] Copies;

        protected override void Bake<K>(ref DynamicBuffer<MagicaClothRenderCopies> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

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
