using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct ActivatedItemAppearance : IComponentData
    {
        public Entity Prefab;
    }

    /// <summary>
    /// TODO при использование SharedComponentDataBehaviour - получаю ошибки при импорте саб-сцены. Возможно, что native SCD не поддерживаются в 0.17
    /// </summary>
    [UseDefaultInspector]
    [DisallowMultipleComponent]
    public class ActivatedItemAppearanceComponent : ComponentDataBehaviour<ActivatedItemAppearance>
    {
        [SerializeField]
        GameObject prefab;

        protected override void Bake<K>(ref ActivatedItemAppearance serializedData, K baker)
        {
            if (prefab != null)
            {
                serializedData.Prefab = baker.GetEntity(prefab);    
            }
        }
    }
}

