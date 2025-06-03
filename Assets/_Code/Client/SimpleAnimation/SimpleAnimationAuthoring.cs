using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System;
using Arena.Client.Anima;
using TzarGames.GameCore.Client;
using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using AnimationState = UnityEngine.AnimationState;

[Serializable]
public class SimpleAnimationClipAuthoring : IAnimationClip
{
    [Serializable]
    public class OtherClipProperties
    {
        public Transform CustomSourceRigPrefab;
        public Avatar CustomSourceAvatar;
    }
    
    public string Label;
    public AnimationClip Clip;
	public AnimationID ID;
    public bool Loop = false;
    public float SpeedScale = 1f;
    public AnimationClipBakeFlags BakeFlags;

    public OtherClipProperties OtherProperties = new();

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

[Serializable]
public struct CopyAnimStatesFrom : IComponentData
{
    public Entity SourceEntity;
}

[DisallowMultipleComponent]
public class SimpleAnimationAuthoring : ComponentDataBehaviourBase
{
    public SimpleAnimationAuthoring OptionalCopyFrom;
    public GameObject AnimationRoot;
    public int DefaultClipIndex = 0;
    public List<SimpleAnimationClipAuthoring> Clips = new();

    class Proxy : IAnimationStateMachineAuthoring
    {
        public GameObject AnimationRoot { get; private set; }
        public int DefaultClipIndex { get; private set; }
        public List<SimpleAnimationClipAuthoring> Clips  { get; private set; }
        public RetargetComponent DefaultRemapper { get; private set; }

        private Dictionary<Avatar, CustomRemapper> remapDataCache = new();

        public Proxy(GameObject animRoot, int defaultClipIndex, List<SimpleAnimationClipAuthoring> clips, RetargetComponent defaultRemapper)
        {
            AnimationRoot = animRoot;
            DefaultClipIndex = defaultClipIndex;
            Clips = clips;
            DefaultRemapper = defaultRemapper;
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
            return AnimationRoot;
            //if (AnimationRoot) return AnimationRoot;
            //return gameObject;
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

        public IRemapper GetRemapperForClip(IAnimationClip clip)
        {
            foreach (var clipAuthoring in Clips)
            {
                if (clip == clipAuthoring)
                {
                    var props = clipAuthoring.OtherProperties;
                    
                    if (props != null &&
                        props.CustomSourceAvatar)
                    {
                        CustomRemapper remapper;

                        if (remapDataCache.TryGetValue(props.CustomSourceAvatar, out remapper) == false)
                        {
                            var remapData = RetargetComponent.CreateRemapData(
                                props.CustomSourceRigPrefab,
                                DefaultRemapper.RetargetRootTransform,
                                props.CustomSourceAvatar, 
                                DefaultRemapper.RetargetAvatar, 
                                DefaultRemapper.RetargetRootTransform,
                                DefaultRemapper.HipsScale);

                            remapper = new CustomRemapper(props.CustomSourceRigPrefab, remapData);
                            remapDataCache.Add(props.CustomSourceAvatar, remapper);
                        }
                        return remapper;
                    }
                    break;
                }
            }
            return DefaultRemapper;
        }

        class CustomRemapper : IRemapper
        {
            public Transform SourceObjectRoot { get; private set; }
            public RemapData RemapData { get; private set; }

            public CustomRemapper(Transform sourceObjectRoot, RemapData remapData)
            {
                SourceObjectRoot = sourceObjectRoot;
                RemapData = remapData;
            }

            public Transform GetSourceObjectRoot()
            {
                return SourceObjectRoot;
            }

            public RemapData GetRemapData()
            {
                return RemapData;
            }
        }
    }

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

        var retarget = GetComponent<RetargetComponent>();
        var clips = OptionalCopyFrom ? OptionalCopyFrom.Clips : Clips;
        var defaultClipIndex = OptionalCopyFrom ? OptionalCopyFrom.DefaultClipIndex : DefaultClipIndex;
        var proxy = new Proxy((AnimationRoot ? AnimationRoot : gameObject), defaultClipIndex, clips, retarget);

        if (OptionalCopyFrom)
        {
            baker.AddComponent(new CopyAnimStatesFrom()
            {
                SourceEntity = baker.GetEntity(OptionalCopyFrom)
            });
        }
        
        AnimationBakingSystem.Bake(proxy, baker.UnityBaker);
#endif
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