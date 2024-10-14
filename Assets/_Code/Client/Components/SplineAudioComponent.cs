using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Splines;

namespace Arena.Client
{
    [Serializable]
    public struct SplineAudio : IComponentData
    {
        public Entity SplineReference;
    }
    
    [RequireComponent(typeof(AudioSource))]
    [UseDefaultInspector]
    public class SplineAudioComponent : ComponentDataBehaviour<SplineAudio>
    {
        public SplineContainer SplineContainer;
        
        protected override void Bake<K>(ref SplineAudio serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            
            if (SplineContainer != null)
            {
                serializedData.SplineReference = baker.GetEntity(SplineContainer.gameObject);    
            }
            
            var audioSource = baker.GetComponent<AudioSource>();
            baker.AddComponentObject(baker.GetEntity(TransformUsageFlags.Dynamic), audioSource);
        }
    }
}