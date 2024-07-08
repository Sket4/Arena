using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    public enum DropAnimationState : byte
    {
        PendingStart,
        Playing,
        Finished
    }

    [System.Serializable]
    public struct DropAnimation : IComponentData
    {
        public float Time;
        public float Height;
        public float RotationSpeed;

        [HideInInspector] public float PlayingTime;
        [HideInInspector] public DropAnimationState State;
        [HideInInspector] public float3 StartTranslation;
        [HideInInspector] public float3 EndTranslation;
        [HideInInspector] public Entity ItemEntity;
    }

    [UseDefaultInspector(true)]
    public class DropAnimationComponent : ComponentDataBehaviour<DropAnimation>
    {
        public GameObject Item;

        protected override void Bake<K>(ref DropAnimation serializedData, K baker)
        {
            if(Item != null)
            {
                serializedData.ItemEntity = baker.GetEntity(Item);
            }

            // отключены по умолчанию
            //baker.AddComponent(new Disabled());
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
