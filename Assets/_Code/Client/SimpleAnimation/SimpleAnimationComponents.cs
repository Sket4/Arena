using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using TzarGames.AnimationFramework;

public struct SimpleAnimation : IComponentData
{
    //public StringHash RootMotionBone;
    public bool IsEnabled;

    public bool IsTransitioning;
    public float RemainingTransitionTime;
    public float TotalTransitionTime;
    public int FromClipIndex;
    public int ToClipIndex;

    [System.NonSerialized]
    public float FromClipSpeed;

    [System.NonSerialized]
    public float ToClipSpeed;

    public bool TransitionTo(int toClip, float duration, float toClipSpeed, ref DynamicBuffer<AnimationState> clipDatas, bool resetTime, bool force = false)
    {
        if (!force && ToClipIndex == toClip)
        {
            return false;
        }

        bool isInstant = duration <= 0f;

        SetWeight(0f, FromClipIndex, ref clipDatas);
        SetWeight(0f, ToClipIndex, ref clipDatas);

        if (!isInstant)
        {
            IsTransitioning = true;
            RemainingTransitionTime = duration;
            TotalTransitionTime = duration;
        }
        else
        {
            IsTransitioning = false;
        }
        
        FromClipIndex = ToClipIndex;
        ToClipIndex = toClip;
        FromClipSpeed = ToClipSpeed;
        ToClipSpeed = toClipSpeed;

        if (isInstant)
        {
            SetWeight(0f, FromClipIndex, ref clipDatas);
            SetWeight(1f, ToClipIndex, ref clipDatas);
        }
        else
        {
            SetWeight(1f, FromClipIndex, ref clipDatas);
            SetWeight(0f, ToClipIndex, ref clipDatas);
        }

        if (resetTime)
        {
            SetTime(0f, 0f, ToClipIndex, ref clipDatas);
        }

        return true;
    }

    //public float GetSpeed(int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    //{
    //    return clipDatas[clipIndex].Speed;
    //}

    public void SetWeight(float value, int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        var i = clipDatas[clipIndex];
        if (value != i.Weight)
        {
            i.Weight = value;
            clipDatas[clipIndex] = i;
        }
    }

    public float GetWeight(int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        return clipDatas[clipIndex].Weight;
    }

    public void SetTime(float value, float deltaTime, int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        var i = clipDatas[clipIndex];
        i.PreviousTime = value;
        i.SetDeltaTime(deltaTime);
        clipDatas[clipIndex] = i;
    }

    public float GetTime(int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        return clipDatas[clipIndex].GetTime();
    }

    public float GetNormalizedTime(int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        float clipDuration = GetClipDuration(clipIndex, ref clipDatas);
        float time = GetTime(clipIndex, ref clipDatas);
        if (clipDatas[clipIndex].Clip.Value.WrapMode == AnimationClipWrapMode.Loop)
        {
            return (time - (clipDuration * math.floor(time / clipDuration))) / clipDuration;
        }
        else
        {
            return time / clipDuration;
        }
    }

    public float GetClipDuration(int clipIndex, ref DynamicBuffer<AnimationState> clipDatas)
    {
        return clipDatas[clipIndex].Clip.Value.Length;
    }

    public static int GetClipIndexByID(DynamicBuffer<ClipIdToIndexMapping> animBuffer, int animID)
    {
        for (int i = 0; i < animBuffer.Length; i++)
        {
            var anim = animBuffer[i];
            if (anim.ID == animID)
            {
                return anim.Index;
            }
        }
        return -1;
    }
    
    public static int GetClipIDByIndex(DynamicBuffer<ClipIdToIndexMapping> animBuffer, int clipIndex)
    {
        for (int i = 0; i < animBuffer.Length; i++)
        {
            var anim = animBuffer[i];
            if (anim.Index == clipIndex)
            {
                return anim.ID;
            }
        }
        return -1;
    }
}

[System.Serializable]
public struct ClipIdToIndexMapping : IBufferElementData
{
    public int ID;
    public int Index;
}

//public struct SimpleAnimationClipData : IBufferElementData
//{
//    public int ID;
//    public BlobAssetReference<Clip> Clip;
//    public NodeHandle<ClipPlayerNode> ClipNode;

//    public bool HasRootMotion;
//    public bool Loop;

//    public float Speed;
//    public bool SpeedDirty;
//    public float Weight;
//    public bool WeightDirty;
//    public float Time;
//    public bool TimeDirty;

//    public static int GetClipIndexByID(DynamicBuffer<SimpleAnimationClipData> animBuffer, int animID)
//    {
//        for (int i = 0; i < animBuffer.Length; i++)
//        {
//            var anim = animBuffer[i];
//            if (anim.ID == animID)
//            {
//                return i;
//            }
//        }
//        return -1;
//    }
//}