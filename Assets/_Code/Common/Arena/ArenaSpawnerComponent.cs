using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using TzarGames.GameCore;
using UnityEngine;

namespace Arena
{
    public enum ArenaSpawnerState
    {
        Idle = 0,
        PendingStart = 1,
        Running = 2,
        WaitingForFinish = 3,
        Finished = 4
    }

    public struct ArenaSpawner : IComponentData
    {
        public ArenaSpawnerState State;
        public uint CurrentGroup;
        public int CurrentObject;
        public int SpawnCounter;
        public double LastSpawnTime;
    }

    public struct SpawnInfoArrayElement : IBufferElementData
    {
        public int GroupIndex;
        public int ObjectIndex;

        public ObjectSpawnInfoParameters Parameters;
    }

    [System.Serializable]
    public struct MessagesToSendOnFinishSpawn : IBufferElementData
    {
        public Message Value;
        public float Delay;
    }

    [System.Serializable]
    struct MessageTargets : IBufferElementData
    {
        public Message Message;
        public Entity Target;
    }

    [UseDefaultInspector]
    public class ArenaSpawnerComponent : ComponentDataBehaviour<ArenaSpawner>
    {
        [System.Serializable]
        public class GroupInfo
        {
            public string Name;
            [NonReorderable]
            public ObjectSpawnInfo[] SpawnInfos;
        }

        [SerializeField]
        [NonReorderable]
        GroupInfo[] groups;

        [System.Serializable]
        class SpawnerMessageAuthoring
        {
            public MessageAuthoring Message;
            public float OptionalDelay = 0;
            public GameObject[] OptionalTargets;
        }

        [SerializeField]
        [NonReorderable]
        SpawnerMessageAuthoring[] MessagesToSendOnFinishSpawn;

        protected override void Bake<K>(ref ArenaSpawner serializedData, K baker)
        {
            var spawnObjectInfos = baker.AddBuffer<SpawnInfoArrayElement>();

            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                var group = groups[groupIndex];

                for (int spawnObjIndex = 0; spawnObjIndex < group.SpawnInfos.Length; spawnObjIndex++)
                {
                    var spawnInfo = group.SpawnInfos[spawnObjIndex];

                    var info = new SpawnInfoArrayElement
                    {
                        GroupIndex = groupIndex,
                        ObjectIndex = spawnObjIndex,
                        Parameters = spawnInfo.SpawnParameters,
                    };

                    var prefab = baker.GetObjectKeyValue(spawnInfo.PrefabKey) as GameObject;
                    if (prefab == null)
                    {
                        Debug.LogErrorFormat("Prefab not found for key {0} in {1}", spawnInfo.PrefabKey.Id, name);
                        continue;
                    }
                    info.Parameters.Prefab = baker.GetEntity(prefab);

                    var spawnPoints = spawnInfo.OptionalSpawnPoints != null ? baker.GetEntity(spawnInfo.OptionalSpawnPoints.gameObject) : Entity.Null;
                    info.Parameters.OptionalSpawnPoints = new SpawnPointArrayReference { Entity = spawnPoints };

                    spawnObjectInfos.Add(info);
                }
            }

            if(MessagesToSendOnFinishSpawn != null)
            {
                var messages = new List<MessagesToSendOnFinishSpawn>();
                var targets = new List<MessageTargets>();

                foreach(var message in MessagesToSendOnFinishSpawn)
                {
                    messages.Add(new MessagesToSendOnFinishSpawn 
                    { 
                        Value = message.Message,
                        Delay = message.OptionalDelay
                    });
                    if(message.OptionalTargets != null)
                    {
                        foreach(var target in message.OptionalTargets)
                        {
                            if(target == null)
                            {
                                continue;
                            }
                            targets.Add(new MessageTargets
                            {
                                Message = message.Message,
                                Target = baker.GetEntity(target)
                            });
                        }
                    }
                }
                var messageBuffer = baker.AddBuffer<MessagesToSendOnFinishSpawn>();
                foreach(var message in messages)
                {
                    messageBuffer.Add(message);
                }

                var targetsBuffer = baker.AddBuffer<MessageTargets>();
                foreach(var target in targets)
                {
                    targetsBuffer.Add(target);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(MessagesToSendOnFinishSpawn == null)
            {
                return;
            }
            var pos = transform.position;

            foreach (var message in MessagesToSendOnFinishSpawn)
            {

                if (message.OptionalTargets != null)
                {
                    foreach (var target in message.OptionalTargets)
                    {
                        if (target == null)
                        {
                            continue;
                        }
                        //Gizmos.color = Color.green;
                        //Gizmos.DrawLine(pos, target.transform.position);
                        UnityEditor.Handles.color = Color.green;
                        UnityEditor.Handles.DrawDottedLine(pos, target.transform.position, 2);
                    }
                }
            }
        }
#endif
    }
}
