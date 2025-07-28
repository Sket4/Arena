using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct CharacterAnimation : IComponentData
    {
        [HideInInspector] public int PendingAnimationID;
        [HideInInspector] public float PendingAnimationDuration;
        [HideInInspector] public float PendingAnimationTransitionTime;
        [HideInInspector] public bool PendingAnimationIsFighting;
        [HideInAuthoring] public double LastFightingAnimationPlayTime;

        [HideInInspector] public int CurrentAnimationID;
        [HideInInspector] public float CurrentAnimationDuration;

        public Entity AnimatorEntity;
        public float FightingStanceTime;
        public float DefaultRunningVelocity;
        public float MaxWalkingVelocity;
    }

#if UNITY_EDITOR
    [UseDefaultInspector(false)]
    public class CharacterAnimationComponent : ComponentDataBehaviour<CharacterAnimation>
    {
        public SimpleAnimationAuthoring Animator;
        public float DefaultRunningVelocity = 1;
        public float FightingStanceTime = 4;
        public float MaxWalkingVelocity = 1;

        protected override void Bake<K>(ref CharacterAnimation animation, K baker)
        {
            animation.CurrentAnimationID = AnimationID.Invalid;
            animation.PendingAnimationID = AnimationID.Invalid;
            animation.FightingStanceTime = FightingStanceTime;
            animation.LastFightingAnimationPlayTime = -9999;

            if (Unity.Mathematics.math.abs(animation.DefaultRunningVelocity) < Unity.Mathematics.math.EPSILON)
            {
                animation.DefaultRunningVelocity = DefaultRunningVelocity;
            }

            animation.MaxWalkingVelocity = MaxWalkingVelocity;
            
            if(Animator != null)
            {
                animation.AnimatorEntity = baker.GetEntity(Animator);
            }
        }
    }
#endif
}
