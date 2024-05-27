using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System;
using TzarGames.GameCore.Client;
using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using AnimationState = UnityEngine.AnimationState;

[Serializable]
public class SimpleAnimationClipAuthoring : IAnimationClip
{
    public AnimationClip Clip;
	public AnimationID ID;
    public bool Loop = false;
    public float SpeedScale = 1f;
    public bool ComputeRootmotionDeltas = false;
    public AnimationClipBakeFlags BakeFlags;

    public AnimationClip GetUnityAnimationClip() => Clip;
    public AnimationClipBakeFlags GetBakeFlags()
    {
        return BakeFlags;
    }
}

[System.Serializable]
public struct AnimationIdData : IComponentData
{
    public int Value;
}

[DisallowMultipleComponent]
public class SimpleAnimationAuthoring : ComponentDataBehaviourBase, IAnimationStateMachineAuthoring
{
    public GameObject RootMotionBone;
    public int DefaultClipIndex = 0;
    public List<SimpleAnimationClipAuthoring> Clips = new List<SimpleAnimationClipAuthoring>();

    protected override void PreBake<T>(T baker)
    {
#if UNITY_EDITOR
        SimpleAnimation simpleAnimaition = new SimpleAnimation
        {
            IsEnabled = true,
            FromClipSpeed = 1.0f,
            ToClipSpeed = 1.0f,
            RemainingTransitionTime = 0.0f,
            IsTransitioning = true,
            FromClipIndex = 0,
            ToClipIndex = DefaultClipIndex,
        };

        //if (rootMotionBone)
        //{
        //    RigComponent rigComponent = gameObject.GetComponent<RigComponent>();
        //    for (var boneIter = 0; boneIter < rigComponent.Bones.Length; boneIter++)
        //    {
        //        if (rootMotionBone.name == rigComponent.Bones[boneIter].name)
        //        {
        //            simpleAnimaition.RootMotionBone = RigGenerator.ComputeRelativePath(rigComponent.Bones[boneIter], rigComponent.transform);
        //        }
        //    }

        //    if (simpleAnimaition.RootMotionBone == default)
        //    {
        //        UnityEngine.Debug.LogError("Root motion bone could not be found");
        //    }

        //    dstManager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(entity);
        //}

        //dstManager.AddComponent<SimpleAnimationDeltaTime>(entity);
        baker.AddComponent(simpleAnimaition);
        //dstManager.AddComponent<DisableRootTransformReadWriteTag>(entity);
        baker.AddBuffer<ClipIdToIndexMapping>();

        AnimationBakingSystem.Bake(this, baker.UnityBaker);
#endif
    }

    public void ConvertAnimationState(int index, ref TzarGames.AnimationFramework.AnimationState state, IAnimationClip originalClip, IBaker baker)
    {
        var clip = originalClip as SimpleAnimationClipAuthoring;
        
        if(clip == null)
        {
            Debug.LogError($"Animation clip is null, baking object: {baker.GetName()}");
            return;
        }

        if(clip.ID == null)
        {
            Debug.LogError($"Animation clip {clip.Clip.name} ID is null, baking object: {baker.GetName()}");
            return;
        }

        state.SpeedScale = clip.SpeedScale;

        if(index == DefaultClipIndex)
        {
            state.Weight = 1;
        }
        baker.AppendToBuffer(new ClipIdToIndexMapping { ID = clip.ID.Id, Index = index });
    }

    public GameObject GetAnimationRootGameObject()
    {
        return gameObject;
    }

    public IAnimationClip[] GetReferencedAnimationClips()
    {
        var list = new List<IAnimationClip>();

        foreach (var clip in Clips)
        {
            list.Add(clip);
        }

        return list.ToArray();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Add animations to the animation component")]
    void addAnimationsToAnimationComponent()
    {
        var animation = GetComponent<Animation>();
        if (animation == null)
        {
            return;
        }

        foreach (var clip in Clips)
        {
            bool isAlreadyAdded = false;

            foreach (AnimationState animState in animation)
            {
                if (animState.clip == clip.Clip)
                {
                    isAlreadyAdded = true;
                    break;
                }
            }

            if (isAlreadyAdded)
            {
                continue;
            }
            
            animation.AddClip(clip.Clip, clip.Clip.name);
        }       
    }
    #endif
}