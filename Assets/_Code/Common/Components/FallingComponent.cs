using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine.Serialization;

namespace Arena
{
    [Serializable]
    public struct Falling : IComponentData
    {
        [FormerlySerializedAs("IsFalling")] [HideInAuthoring] public bool IsInAir;
        [HideInAuthoring] public float FallingStartHeight;
    }
    public class FallingComponent : ComponentDataBehaviour<Falling>
    {
    }
}
