using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct PlayerAbilityInfo
    {
        [HideInAuthoring] public AbilityID ID;
        [HideInAuthoring] public Entity Ability;
    }
    
    [Serializable]
    public struct PlayerAbilities : IComponentData
    {
        [HideInAuthoring] public PlayerAbilityInfo AttackAbility;
        [HideInAuthoring] public PlayerAbilityInfo Ability1;
        [HideInAuthoring] public PlayerAbilityInfo Ability2;
        [HideInAuthoring] public PlayerAbilityInfo Ability3;
    }

    public class PlayerAbilitiesComponent : ComponentDataBehaviour<PlayerAbilities>
    {
    }
}
