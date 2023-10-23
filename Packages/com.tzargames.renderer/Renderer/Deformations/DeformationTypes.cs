using Unity.Entities;
using Unity.Mathematics;

namespace TzarGames.Renderer
{
    [System.Serializable]
    public struct BoneArrayElement : IBufferElementData
    {
        public Entity Entity;
    }

    [System.Serializable]
    public struct RootBone : IComponentData
    {
        public Entity Entity;
    }

    [System.Serializable]
    public struct BindPoseMatrix : IBufferElementData
    {
        public float4x4 Value;
    }
}
