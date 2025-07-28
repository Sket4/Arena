using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.Baking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Abilities
{
    [System.Serializable]
    public struct AnimationAbilityComponentData : IComponentData, IAbilityComponentJob<AnimationAbilityComponentStartJob>
    {
        public bool UseAbilityDuration;
        public bool UseCustomTransitionTime;
        public float CustomTransitionTime;
        public bool IsFightingAnimation;
    }

    public struct AnimationAbilityStopComponentData : IComponentData, IAbilityComponentJob<AnimationAbilityComponentStopJob>
    {
        public int PlayingAnimationID;
    }

    [RequireComponent(typeof(AnimationArrayElementComponent))]
    [DisallowMultipleComponent]
    public class AnimationAbilityComponent : ComponentDataBehaviourBase
    {
        [SerializeField]
        bool durationAsAnimDuration = false;

        [SerializeField] private bool isFightingAnimation = false;
        [SerializeField] private bool useCustomTransitionTime = false;
        [SerializeField] private float customTransitionTime = 0.2f;

        [SerializeField]
        bool cancelAnimationOnStop = false;

        protected override void PreBake<T>(T baker)
        {
            if (ShouldBeConverted(baker) == false)
            {
                return;
            }

            baker.AddComponent(new AnimationAbilityComponentData
            {
                UseAbilityDuration = durationAsAnimDuration,
                UseCustomTransitionTime = useCustomTransitionTime,
                CustomTransitionTime = customTransitionTime,
                IsFightingAnimation = isFightingAnimation
            });
            if (cancelAnimationOnStop)
            {
                baker.AddComponent<AnimationAbilityStopComponentData>();
            }
        }

        static AnimationAbilityComponentStartJob createStartJob(AbilitySystem system)
        {
            return new AnimationAbilityComponentStartJob
            {
                StopAnimType = system.GetComponentTypeHandle<AnimationAbilityStopComponentData>(),
                DurationType = system.GetComponentTypeHandle<Duration>(true),
            };
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }

    [Unity.Burst.BurstCompile]
    public struct AnimationAbilityComponentStartJob 
    {
        public ComponentTypeHandle<AnimationAbilityStopComponentData> StopAnimType;

        [ReadOnly]
        public ComponentTypeHandle<Duration> DurationType;

        [MayReadComponents(typeof(Duration), typeof(AnimationAbilityStopComponentData))]
        [MethodPriority(AbilitySystem.DefaultLowestPriority)]
        public void OnStarted(
            in AbilityInterface abilityData, 
            float deltaTime, 
            in AbilityOwner abilityOwner, 
            int commandBufferIndex, 
            EntityCommandBuffer.ParallelWriter commands, 
            in AnimationAbilityComponentData component, 
            DynamicBuffer<Animations> animations
            )
        {
            if(animations.Length == 0)
            {
                Debug.LogError("Animation list is empty");
                return;
            }

            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(abilityOwner.Value.Index + (int)(deltaTime * 100000)));

            var anim = animations[random.NextInt(0, animations.Length)];
            var animId = anim.ID;
            var animEvent = commands.CreateEntity(commandBufferIndex);

            if(abilityData.HasComponent(StopAnimType))
            {
                abilityData.SetComponent(StopAnimType, new AnimationAbilityStopComponentData 
                {
                    PlayingAnimationID = animId
                });
            }

            commands.AddComponent(commandBufferIndex, animEvent, new CharacterAnimationPlayEvent 
            { 
                Target = abilityOwner.Value,
                AnimationID = animId,
                UseTransitionTime = component.UseCustomTransitionTime,
                TransitionTime = component.CustomTransitionTime,
            });
            if (component.IsFightingAnimation)
            {
                commands.AddComponent(commandBufferIndex, animEvent, new FightingAnimationTag());
            }
            if(component.UseAbilityDuration && abilityData.HasComponent(DurationType))
            {
                var duration = abilityData.GetComponent(DurationType);
                commands.AddComponent(commandBufferIndex, animEvent, duration); 
            }
        }
    }

    public struct AnimationAbilityComponentStopJob
    {
        [MethodPriority(AbilitySystem.DefaultLowestPriority)]
        public void OnStopped(in AnimationAbilityStopComponentData component, in AbilityOwner abilityOwner, int commandBufferIndex, EntityCommandBuffer.ParallelWriter commands)
        {
            var animEvent = commands.CreateEntity(commandBufferIndex);

            commands.AddComponent(commandBufferIndex, animEvent, new CharacterAnimationStopEvent 
            { 
                Target = abilityOwner.Value,
                AnimationID = component.PlayingAnimationID
            });
        }
    }
}
