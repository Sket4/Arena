using System.Collections;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Server
{
    [System.Serializable]
    public struct PrefabSpawnPoint : IComponentData
    {
        public int PrefabID;
    }

    [UseDefaultInspector]
    public class PrefabSpawnPointComponent : ComponentDataBehaviour<PrefabSpawnPoint>
    {
        [SerializeField]
        ObjectKey prefab;
        
        public ObjectKey Prefab
        {
            get
            {
                return prefab;
            }
        }

        public int PrefabID
        {
            get
            {
                return prefab.Id;
            }
        }

        protected override void Bake<K>(ref PrefabSpawnPoint serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if (prefab == null)
            {
                Debug.LogError("Prefab is null");
                return;
            }
            serializedData.PrefabID = prefab.Id;
        }
    }
}
