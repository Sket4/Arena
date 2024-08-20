using TzarGames.GameCore;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct EnemyDetectionSettings : IComponentData
    {
        public float DetectionRadius;
        public uint TraceLayers;
    }

    public struct EnemyDetectionData : IComponentData
    {
        public bool HasNearEnemy;
    }

    [UseDefaultInspector]
    public class EnemyDetectorComponent : ComponentDataBehaviour<EnemyDetectionSettings>
    {
        public float DetectionRadius = 15;
        public LayerMask TraceLayers;


        protected override void Bake<K>(ref EnemyDetectionSettings serializedData, K baker)
        {
            serializedData.DetectionRadius = DetectionRadius;
            serializedData.TraceLayers = Utility.LayerMaskToCollidesWithMask(TraceLayers);

            baker.AddComponent(new EnemyDetectionData());
        }
    }
}
