using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace TzarGames.GameCore
{
    [System.Serializable]
    public struct LevelUpEffect : IComponentData
    {
        public Entity Prefab;
    }

    [UseDefaultInspector]
    public class LevelUpEffectComponent : ComponentDataBehaviour<LevelUpEffect>
    {
        [SerializeField]
        GameObject prefab;

        protected override void Bake<K>(ref LevelUpEffect serializedData, K baker)
        {
            serializedData.Prefab = baker.GetEntity(prefab);
        }
    }
}
