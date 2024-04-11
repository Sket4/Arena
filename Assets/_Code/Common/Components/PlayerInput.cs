using TzarGames.GameCore.Abilities;
using Unity.Entities;
using Unity.Mathematics;

namespace Arena
{
    public struct PlayerInput : IComponentData
    {
        public AbilityID PendingAbilityID;
        public float Horizontal;
        public float Vertical;
        public float2 ViewScroll;
    }
}
