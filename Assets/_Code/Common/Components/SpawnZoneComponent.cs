using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    public struct SpawnZoneParameters : IComponentData
    {
        public int MaximumSpawnCount;
        public float SpawnRadius;
        public float SpawnInterval;
        public float SpawnAfterDeathInverval;

        public bool DisableRespawn;
        public bool SendMessageOnAllDead;
        [HideInAuthoring]
        public Message AllDeadMessage;
        
        [HideInAuthoring]
        public uint SpawnPointTraceLayers;
        
        [HideInAuthoring]
        public Entity Prefab;
    }

    public struct SpawnZoneStateData : IComponentData, IEnableableComponent
    {
        public double LastSpawnTime;
    }

    [InternalBufferCapacity(32)]
    public struct SpawnZoneInstance : IBufferElementData
    {
        public Entity Value;
        public double DestroyTime;
    }
    
    [UseDefaultInspector(true)]
    [RequireComponent(typeof(RadiusComponent), typeof(HeightComponent))]
    public class SpawnZoneComponent : ComponentDataBehaviour<SpawnZoneParameters>
    {
        public CharacterKey CharacterPrefabKey;
        public LayerMask SpawnPointTraceLayers;
        public MessageAuthoring AllDeadMessage;
        
        protected override void Bake<K>(ref SpawnZoneParameters serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            serializedData.Prefab = baker.ConvertObjectKey(CharacterPrefabKey);
            serializedData.SpawnPointTraceLayers = Utility.LayerMaskToCollidesWithMask(SpawnPointTraceLayers);
            serializedData.AllDeadMessage = AllDeadMessage;
            
            baker.AddBuffer<SpawnZoneInstance>();
            baker.AddComponent<SpawnZoneStateData>();
        }

        protected override SpawnZoneParameters CreateDefaultValue()
        {
            return new SpawnZoneParameters
            {
                SpawnRadius = 10,
                MaximumSpawnCount = 15,
                SpawnInterval = 1,
                SpawnAfterDeathInverval = 60
            };
        }

        #if UNITY_EDITOR
        private static System.Collections.Generic.Dictionary<object, RadiusComponent> cachedRadiuses = new(); 
        private static System.Collections.Generic.Dictionary<object, HeightComponent> cachedHeights = new();
        
        private void OnDrawGizmos()
        {
            if (cachedRadiuses.TryGetValue(this, out var radius) == false)
            {
                radius = GetComponent<RadiusComponent>();

                if (radius != null)
                {
                    cachedRadiuses.Add(this, radius);    
                }
            }

            if (radius == null)
            {
                return;
            }
            
            if (cachedHeights.TryGetValue(this, out var height) == false)
            {
                height = GetComponent<HeightComponent>();

                if (height != null)
                {
                    cachedHeights.Add(this, height);    
                }
            }

            if (height == null)
            {
                return;
            }

            var pos = transform.position;
            var heightDisp = Vector3.down * height.Value.Value;
            var spawnRad = Value.SpawnRadius;
            var rad = radius.Value.Value;
            
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(pos, Vector3.up, spawnRad);
            UnityEditor.Handles.DrawWireDisc(pos + heightDisp, Vector3.up, spawnRad);
            
            var lines = new[]
            {
                pos + Vector3.right * spawnRad,
                pos + Vector3.right * spawnRad + heightDisp,
                    
                pos - Vector3.right * spawnRad,
                pos - Vector3.right * spawnRad + heightDisp,
                    
                pos + Vector3.forward * spawnRad,
                pos + Vector3.forward * spawnRad + heightDisp,
                    
                pos - Vector3.forward * spawnRad,
                pos - Vector3.forward * spawnRad + heightDisp,
                    
                    
                pos + (Vector3.forward + Vector3.right).normalized * spawnRad,
                pos + (Vector3.forward + Vector3.right).normalized * spawnRad + heightDisp,
                    
                pos + (Vector3.forward + Vector3.left).normalized * spawnRad,
                pos + (Vector3.forward + Vector3.left).normalized * spawnRad + heightDisp,
                    
                pos - (Vector3.forward + Vector3.right).normalized * spawnRad,
                pos - (Vector3.forward + Vector3.right).normalized * spawnRad + heightDisp,
                    
                pos - (Vector3.forward + Vector3.left).normalized * spawnRad,
                pos - (Vector3.forward + Vector3.left).normalized * spawnRad + heightDisp,
            };
            
            UnityEditor.Handles.DrawLines(lines);
        }
        #endif
    }
}
