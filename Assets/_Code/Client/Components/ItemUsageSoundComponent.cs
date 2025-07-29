using System;
using TzarGames.GameCore;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct ItemUsageSound : IComponentData
    {
        public WeakObjectReference<AudioClip> ClipRef;
    }

    [UseDefaultInspector]
    public class ItemUsageSoundComponent : ComponentDataBehaviour<ItemUsageSound>
    {
        public AudioClip Clip;

        #if UNITY_EDITOR
        protected override void Bake<K>(ref ItemUsageSound serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.ClipRef = new WeakObjectReference<AudioClip>(Clip);
        }
        #endif
    }
}
