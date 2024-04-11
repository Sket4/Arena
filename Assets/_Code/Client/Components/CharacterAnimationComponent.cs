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

        [HideInInspector] public int CurrentAnimationID;
        [HideInInspector] public float CurrentAnimationDuration;

        public Entity AnimatorEntity;
        public int IdleAnimID;
        public int RunningAnimID;
        public int DeathAnimationID;
        public float DefaultRunningVelocity;
    }

#if UNITY_EDITOR
    [UseDefaultInspector(false)]
    public class CharacterAnimationComponent : ComponentDataBehaviour<CharacterAnimation>
    {
        public SimpleAnimationAuthoring Animator;
        public AnimationID IdleAnimationID;
        public AnimationID RunningAnimationID;
        public float DefaultRunningVelocity = 1;

        [Header("Optional settings")]
        public AnimationID DeathAnimationID;

        protected override void Bake<K>(ref CharacterAnimation animation, K baker)
        {
            animation.CurrentAnimationID = AnimationID.Invalid;
            animation.PendingAnimationID = AnimationID.Invalid;
            animation.IdleAnimID = IdleAnimationID != null ? IdleAnimationID.Id : AnimationID.Invalid;
            animation.RunningAnimID = RunningAnimationID != null ? RunningAnimationID.Id : AnimationID.Invalid;
            animation.DeathAnimationID = DeathAnimationID != null ? DeathAnimationID.Id : AnimationID.Invalid;

            if (Unity.Mathematics.math.abs(animation.DefaultRunningVelocity) < Unity.Mathematics.math.EPSILON)
            {
                animation.DefaultRunningVelocity = DefaultRunningVelocity;
            }
            
            if(Animator != null)
            {
                animation.AnimatorEntity = baker.GetEntity(Animator);
            }
        }
    }
#endif
}
