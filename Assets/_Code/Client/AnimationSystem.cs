using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Arena.Client
{

    public struct CharacterAnimationPlayEvent : IComponentData
    {
        public Entity Target;
        public int AnimationID;
        public bool UseTransitionTime;
        public float TransitionTime;
    }

    public struct FightingAnimationTag : IComponentData
    {
    }

    public struct CharacterAnimationStopEvent : IComponentData
    {
        public Entity Target;
        public int AnimationID;
    }
    
    [DisableAutoCreation]
    [UpdateBefore(typeof(SimpleAnimationSystem))]
    public partial class AnimationSystem : GameSystemBase
    {
        private const int IdleAnimationID = 2;
        private const int FightingStanceAnimationID = 24;
        private const int RunningAnimationID = 3;
        private const int WalkingAnimationID = 20;
        private const int JumpLoopAnimationID = 22;

        private ComponentLookup<Unity.CharacterController.KinematicCharacterBody> BodyLookup;

        protected override void OnCreate()
        {
            base.OnCreate();
            BodyLookup = GetComponentLookup<Unity.CharacterController.KinematicCharacterBody>(true);
        }

        protected override void OnSystemUpdate()
        {
            var commands = CommandBufferSystem.CreateCommandBuffer();

            updateGenericAnimation(commands);
            updateCharacterAnimation(commands);
        }

        private void updateGenericAnimation(EntityCommandBuffer commands)
        {
            // PLAY ANIM EVENTS
            Entities.ForEach((Entity entity, in AnimationPlayEvent playEvent, in Target target) =>
            {
                if(playEvent.AutoDestroy)
                {
                    commands.DestroyEntity(entity);
                }

                Debug.Log($"Trying to play anim event {playEvent.AnimationID} on target {target.Value.Index}");

                if(SystemAPI.HasComponent<SimpleAnimation>(target.Value) == false)
                {
                    Debug.LogError($"Animation play failed, no anim component on entity {target.Value.Index}");
                    return;
                }

                var animation = SystemAPI.GetComponent<SimpleAnimation>(target.Value);
                var animToIndexBuffer = SystemAPI.GetBuffer<ClipIdToIndexMapping>(target.Value);
                
                var index = SimpleAnimation.GetClipIndexByID(animToIndexBuffer, playEvent.AnimationID);

                if(index == -1)
                {
                    Debug.LogError($"Failed to find animation with id {playEvent.AnimationID} on entity {target.Value.Index}");
                    return;
                }

                var animStates = SystemAPI.GetBuffer<TzarGames.AnimationFramework.AnimationState>(target.Value);

                animation.TransitionTo(index, 0, 1.0f, ref animStates, true);
                SystemAPI.SetComponent(target.Value, animation);

            }).Schedule();
        }

        private void updateCharacterAnimation(EntityCommandBuffer commands)
        {
            Entities.ForEach((Entity entity, in CharacterAnimationPlayEvent playEvent) =>
            {
                var animation = SystemAPI.GetComponent<CharacterAnimation>(playEvent.Target);
                animation.PendingAnimationID = playEvent.AnimationID;

                if (SystemAPI.HasComponent<TzarGames.GameCore.Abilities.Duration>(entity))
                {
                    var duration = SystemAPI.GetComponent<TzarGames.GameCore.Abilities.Duration>(entity);
                    animation.PendingAnimationDuration = duration.Value;
                }
                else
                {
                    animation.PendingAnimationDuration = 0;
                }

                if (playEvent.UseTransitionTime)
                {
                    animation.PendingAnimationTransitionTime = playEvent.TransitionTime;
                }
                else
                {
                    animation.PendingAnimationTransitionTime = -1;
                }

                if (SystemAPI.HasComponent<FightingAnimationTag>(entity))
                {
                    animation.PendingAnimationIsFighting = true;
                }

                SystemAPI.SetComponent(playEvent.Target, animation);
                commands.DestroyEntity(entity);

            }).Schedule();

            Entities.ForEach((Entity entity, in CharacterAnimationStopEvent playEvent) =>
            {
                var animation = SystemAPI.GetComponent<CharacterAnimation>(playEvent.Target);
                if (animation.CurrentAnimationID == playEvent.AnimationID)
                {
                    animation.CurrentAnimationID = AnimationID.Invalid;
                }
                if (animation.PendingAnimationID == playEvent.AnimationID)
                {
                    animation.PendingAnimationID = AnimationID.Invalid;
                }

                SystemAPI.SetComponent(playEvent.Target, animation);
                commands.DestroyEntity(entity);

            }).Schedule();

            var bodyLookup = BodyLookup;
            bodyLookup.Update(this);
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            
            Entities
                .WithReadOnly(bodyLookup)
                .ForEach((Entity entity, ref CharacterAnimation animation, in AttackSpeed attackSpeed, in Velocity velocity, in LivingState livingState) =>
                {
                    if (SystemAPI.HasComponent<SimpleAnimation>(animation.AnimatorEntity) == false)
                    {
                        return;
                    }

                    var animator = SystemAPI.GetComponent<SimpleAnimation>(animation.AnimatorEntity);

                    if(animator.IsEnabled == false)
                    {
                        return;
                    }

                    var animBuffer = SystemAPI.GetBuffer<TzarGames.AnimationFramework.AnimationState>(animation.AnimatorEntity);
                    var clipIdToIndexBuffer = SystemAPI.GetBuffer<ClipIdToIndexMapping>(animation.AnimatorEntity);

                    float transitionDuration = 0.3f;

                    // if (livingState.IsAlive == false)
                    // {
                    //     animation.PendingAnimationID = AnimationID.Invalid;
                    //     animation.CurrentAnimationID = AnimationID.Invalid;
                    // }

                    bool resetCurrentAnimation = false;

                    if (animation.PendingAnimationID != AnimationID.Invalid)
                    {
                        animation.CurrentAnimationID = animation.PendingAnimationID;
                        resetCurrentAnimation = true;
                        animation.PendingAnimationID = AnimationID.Invalid;

                        if (animation.PendingAnimationDuration > 0)
                        {
                            animation.CurrentAnimationDuration = animation.PendingAnimationDuration;
                            animation.PendingAnimationDuration = 0;
                        }
                        else
                        {
                            animation.CurrentAnimationDuration = 0;
                        }

                        if (animation.PendingAnimationTransitionTime >= 0.0f)
                        {
                            transitionDuration = animation.PendingAnimationTransitionTime;
                            animation.PendingAnimationTransitionTime = -1;
                        }

                        if (animation.PendingAnimationIsFighting)
                        {
                            animation.LastFightingAnimationPlayTime = elapsedTime;
                            animation.PendingAnimationIsFighting = false;
                        }

                        //Debug.Log($"Play anim {animation.CurrentAnimationID} {animation.CurrentAnimationDuration}");
                    }

                    var idleAnimIndex = SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, IdleAnimationID);
                    var fightingStanceAnimIndex =
                        SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, FightingStanceAnimationID);

                    if (animation.CurrentAnimationID != AnimationID.Invalid)
                    {
                        var currentAnimIndex = SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, animation.CurrentAnimationID);

                        if (currentAnimIndex != -1)
                        {
                            if (animator.ToClipIndex != currentAnimIndex || resetCurrentAnimation)
                            {
                                // перехожим в анимацию CurrentAnimationID
                                float speedScale = 1;
                                if (animation.CurrentAnimationDuration > math.EPSILON)
                                {
                                    var clipDuration = animator.GetClipDuration(currentAnimIndex, ref animBuffer);
                                    speedScale = clipDuration / animation.CurrentAnimationDuration;
                                }
                                
                                transitionDuration = math.min(transitionDuration, transitionDuration / speedScale);
                                
                                //Debug.Log($"Transiting to {currentAnimIndex} with duration {transitionDuration}, fromspd {animator.FromClipSpeed} tospd {animator.ToClipSpeed}, nextspd {speedScale}");
                                animator.TransitionTo(currentAnimIndex, transitionDuration, speedScale, ref animBuffer, true, true);
                                SystemAPI.SetComponent(animation.AnimatorEntity, animator);
                                return;
                            }
                            else
                            {
                                // уже проигрываем анимацию CurrentAnimationID, поэтому проверяем, не закончилась ли она
                                var time = animator.GetNormalizedTime(currentAnimIndex, ref animBuffer);

                                if (time >= 1.0f)
                                {
                                    animation.CurrentAnimationID = AnimationID.Invalid;
                                    // пропускаем один фрейм и не переходим в анимацию по умолчанию, на случай если в следующем кадре придет запрос на следующую анимацию
                                    //return;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to find animation with id {animation.CurrentAnimationID} in entity {animation.AnimatorEntity.Index}");
                            animation.CurrentAnimationID = AnimationID.Invalid;
                        }
                    }

                    if (livingState.IsAlive)
                    {
                        bool isGrounded;

                        if (bodyLookup.TryGetComponent(entity, out var body))
                        {
                            isGrounded = body.IsGrounded;
                        }
                        else
                        {
                            isGrounded = false;
                        }
                        
                        var isInAir = isGrounded == false;
                        var isPlayingAirAnimation = false;
                        
                        if (isInAir)
                        {
                            var jumpLoopClipIndex =
                                SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, JumpLoopAnimationID);

                            if (jumpLoopClipIndex >= 0)
                            {
                                animator.TransitionTo(jumpLoopClipIndex, 0.15f, 1, ref animBuffer, false);
                                isPlayingAirAnimation = true;
                            }
                        }

                        if (isPlayingAirAnimation == false)
                        {
                            if (velocity.CachedMagnitude > 0.01f)
                            {
                                int animID;
                                float defaultAnimSpeed;

                                if(velocity.CachedMagnitude <= animation.MaxWalkingVelocity
                                   && SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, WalkingAnimationID) >= 0)
                                {
                                    animID = WalkingAnimationID;
                                    defaultAnimSpeed = animation.MaxWalkingVelocity;
                                }
                                else
                                {
                                    animID = RunningAnimationID;
                                    defaultAnimSpeed = animation.DefaultRunningVelocity;
                                }

                                if (defaultAnimSpeed <= 0)
                                {
                                    defaultAnimSpeed = 1;
                                }
                                
                                var animIndex = SimpleAnimation.GetClipIndexByID(clipIdToIndexBuffer, animID);

                                if (animIndex != AnimationID.Invalid)
                                {
                                    var speed = velocity.CachedMagnitude / defaultAnimSpeed;
                                    speed = math.max(speed, 0.1f);

                                    //if(animator.ToClipIndex !- animIndex)
                                    //{
                                    //Debug.Log($"Transiting to run, fromspd {animator.FromClipSpeed} tospd {animator.ToClipSpeed}, nextspd {speed}");
                                    //}

                                    animator.TransitionTo(animIndex, transitionDuration, speed, ref animBuffer, false);
                                    animator.ToClipSpeed = speed;    
                                }
                                else
                                {
                                    Debug.LogError($"Failed to find animation with id {RunningAnimationID} in entity {animation.AnimatorEntity.Index}");
                                }
                            }
                            else
                            {
                                if (idleAnimIndex != -1)
                                {
                                    //if(animator.ToClipIndex != idleAnimIndex)
                                    //{
                                    //Debug.Log($"Transiting to idle, fromspd {animator.FromClipSpeed} tospd {animator.ToClipSpeed}, nextspd {1.0f}");
                                    //}
                                    
                                    var fightingStanceTime = elapsedTime - animation.LastFightingAnimationPlayTime;
                                    
                                    if (fightingStanceTime < animation.FightingStanceTime && fightingStanceAnimIndex != -1)
                                    {
                                        animator.TransitionTo(fightingStanceAnimIndex, transitionDuration, 1.0f, ref animBuffer, false);
                                        animator.ToClipSpeed = 1.0f;   
                                    }
                                    else
                                    {
                                        animator.TransitionTo(idleAnimIndex, transitionDuration, 1.0f, ref animBuffer, false);
                                        animator.ToClipSpeed = 1.0f;    
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"Failed to find animation with id {IdleAnimationID} in entity {animation.AnimatorEntity.Index}");
                                }
                            }
                        }
                    }
                    SystemAPI.SetComponent(animation.AnimatorEntity, animator);

                }).Schedule();
        }
    }
}
