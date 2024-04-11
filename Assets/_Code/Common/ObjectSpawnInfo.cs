// Copyright 2020 Dinar Khasanov (E-mail: lespaul@live.ru) All Rights Reserved.
using Unity.Entities;

namespace TzarGames.GameCore
{
    [System.Serializable]
    public class ObjectSpawnInfo
    {
        public ObjectKey PrefabKey;
        public SpawnPointArrayComponent OptionalSpawnPoints;

        public ObjectSpawnInfoParameters SpawnParameters = new ObjectSpawnInfoParameters
        {
            Count = 5,
            NextDelay = 1,
            SpawnInterval = 1
        };
    }

    [System.Serializable]
    public struct ObjectSpawnInfoParameters
    {
        [UnityEngine.HideInInspector]
        public Entity Prefab;

        public uint Count;
        public float SpawnInterval;
        public float NextDelay;

        [System.NonSerialized]
        public SpawnPointArrayReference OptionalSpawnPoints;
    }
}
