#if ARENA_TEST || UNITY_EDITOR
using Unity.Entities;
using Unity.Mathematics;

namespace TzarGames.GameCore.Tests
{
    [System.Serializable]
    public struct TestForwardBackMover : IComponentData
    {
        public float MoveTime;
        public float Speed;
        public float3 Direction;
        [System.NonSerialized]
        public double LastTime;
    }

    public class TestMover : ComponentDataBehaviour<TestForwardBackMover>
    {
    }
}
#endif