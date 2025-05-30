using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct WaterEffects : IComponentData
    {
        public Entity SplashPrefab;
        public Entity RipplesPrefab;
    }
    
    [UseDefaultInspector]
    public class WaterEffectsComponent : ComponentDataBehaviour<WaterEffects>
    {
        public GameObject SplashPrefab;
        public GameObject RipplesPrefab;

        protected override void Bake<K>(ref WaterEffects serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.SplashPrefab = baker.GetEntity(SplashPrefab);
            serializedData.RipplesPrefab = baker.GetEntity(RipplesPrefab);
        }
    }
}
