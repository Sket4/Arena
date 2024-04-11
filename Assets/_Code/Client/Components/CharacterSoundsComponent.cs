using System.Collections.Generic;
using UnityEngine;
using TzarGames.GameCore;
using Unity.Entities;
using TzarGames.GameCore.Baking;
using Unity.Entities.Content;
using Unity.Entities.Serialization;

namespace Arena.Client
{
    [System.Serializable]
    public struct CharacterDeadAudioClip : IBufferElementData
    {
        public WeakObjectReference<AudioClip> AudioClip;
    }

    [UseDefaultInspector]
    public class CharacterSoundsComponent : DynamicBufferBehaviour<CharacterDeadAudioClip>
    {
        [System.Serializable]
        class CharacterDeadAudioClipAuthoring
        {
            public AudioClip AudioClip;
        }

        [SerializeField]
        [NonReorderable]
        CharacterDeadAudioClipAuthoring[] deadAudioClips;

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }

        protected override void Bake<K>(ref DynamicBuffer<CharacterDeadAudioClip> serializedData, K baker)
        {
#if UNITY_EDITOR
            foreach(var c in deadAudioClips)
            {
                serializedData.Add(new CharacterDeadAudioClip
                {
                    AudioClip = new WeakObjectReference<AudioClip>(c.AudioClip),
                });
            }
#endif
        }
    }
}
