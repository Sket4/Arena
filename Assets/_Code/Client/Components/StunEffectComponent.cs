using UnityEngine;
using Unity.Entities;

namespace TzarGames.GameCore
{
    [System.Serializable]
    public struct StunEffect : IComponentData, System.IEquatable<StunEffect>
    {
        public Entity Prefab;

        public bool Equals(StunEffect other)
        {
            return ReferenceEquals(Prefab, other.Prefab);
        }

        public override int GetHashCode()
        {
            return Prefab.GetHashCode();
        }
    }

    public class StunEffectComponent : ComponentDataBehaviour<StunEffect>
    {
    }
}
