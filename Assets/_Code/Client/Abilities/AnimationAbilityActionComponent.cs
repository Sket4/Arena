using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Client;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Abilities
{
    [System.Serializable]
    [InternalBufferCapacity(4)]
    public struct AnimationAbilityAction : IAbilityAction, IBufferElementData, IAbilityActionJob<AnimationAbilityActionJob>
    {
        public int AnimationID;
        [SerializeField]
        private int callerId;
        public int CallerId { get =>callerId; set => callerId = value; }

        [SerializeField]
        private byte actionId;
        public byte ActionId { get => actionId; set => actionId = value; }
    }

    public class AnimationAbilityActionComponent : AbilityActionAsset<AnimationAbilityAction>
    {
        [SerializeField] private AnimationID animationID;

        protected override AnimationAbilityAction Convert(IGCBaker baker)
        {
            return new AnimationAbilityAction { AnimationID = animationID.Id };
        }
    }

    struct AnimationAbilityActionJob
    {
        public UniversalEntityCommandBuffer Commands;
        
        public void Execute(in AbilityOwner abilityOwner, int jobIndex, AnimationAbilityAction eventData)
        {
            var animEvent = new CharacterAnimationPlayEvent();
            animEvent.AnimationID = eventData.AnimationID;
            animEvent.Target = abilityOwner.Value;
            
            var evt = Commands.CreateEntity(jobIndex);
            Commands.AddComponent(jobIndex, evt, animEvent);
        }
    }
}
