using TzarGames.AnimationFramework;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateBefore(typeof(AnimationSystemGroup))]
public partial class SimpleAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        CompleteDependency();

        float deltaTime = World.Time.DeltaTime;

        Entities
            .ForEach((
                Entity entity,
                DynamicBuffer<AnimationState> animStates,
                ref SimpleAnimation simpleAnimation
                ) =>
            {
                if(simpleAnimation.IsEnabled == false)
                {
                    return;
                }

                // Handle transitions
                if (simpleAnimation.IsTransitioning && animStates.Length > 0)
                {
                    simpleAnimation.RemainingTransitionTime -= deltaTime;
                    float normalizedTransitionTime = math.clamp(1f - (simpleAnimation.RemainingTransitionTime / simpleAnimation.TotalTransitionTime), 0f, 1f);

                    simpleAnimation.SetWeight(1f - normalizedTransitionTime, simpleAnimation.FromClipIndex, ref animStates);
                    simpleAnimation.SetWeight(normalizedTransitionTime, simpleAnimation.ToClipIndex, ref animStates);

                    if (simpleAnimation.RemainingTransitionTime <= 0f)
                    {
                        simpleAnimation.IsTransitioning = false;
                    }
                }

                for (int i = 0; i < animStates.Length; ++i)
                {
                    var clipData = animStates[i];

                    if(clipData.Weight < float.Epsilon)
                    {
                        continue;
                    }
                    float speedScale;

                    if(simpleAnimation.FromClipIndex == i)
                    {
                        speedScale = simpleAnimation.FromClipSpeed;
                    }
                    else if(simpleAnimation.ToClipIndex == i)
                    {
                        speedScale = simpleAnimation.ToClipSpeed;
                    }
                    else
                    {
                        speedScale = 1.0f;
                    }

                    clipData.AddDeltaTime(deltaTime * speedScale);

                    animStates[i] = clipData;
                }

            }).Run();
    }

    public static void NormalizeWeights(in SimpleAnimation simpleAnimation, ref DynamicBuffer<AnimationState> animationClipDataBuffer)
    {
        float totalWeight = 0f;
        for (int i = 0; i < animationClipDataBuffer.Length; i++)
        {
            totalWeight += animationClipDataBuffer[i].Weight;
        }

        if (totalWeight <= 0f)
        {
            simpleAnimation.SetWeight(1f, 0, ref animationClipDataBuffer);
        }
        else
        {
            for (int i = 0; i < animationClipDataBuffer.Length; i++)
            {
                float weight = simpleAnimation.GetWeight(i, ref animationClipDataBuffer);
                if (weight > 0f)
                {
                    simpleAnimation.SetWeight(weight / totalWeight, i, ref animationClipDataBuffer);
                }
            }
        }
    }
}
