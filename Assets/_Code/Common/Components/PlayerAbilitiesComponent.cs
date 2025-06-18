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

    [Serializable]
    public struct AbilityPoints : IComponentData
    {
        [HideInAuthoring] public ushort Count;
    }

    public class PlayerAbilitiesComponent : ComponentDataBehaviour<PlayerAbilities>
    {
        protected override void Bake<K>(ref PlayerAbilities serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent(new AbilityPoints());
        }
    }
}
