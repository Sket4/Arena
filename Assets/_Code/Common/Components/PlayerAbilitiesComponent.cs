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

        public void Reset()
        {
            ID = AbilityID.Null;
            Ability = Entity.Null;
        }
    }
    
    [Serializable]
    public struct PlayerAbilities : IComponentData
    {
        [HideInAuthoring] public PlayerAbilityInfo AttackAbility;
        [HideInAuthoring] public PlayerAbilityInfo Ability1;
        [HideInAuthoring] public PlayerAbilityInfo Ability2;
        [HideInAuthoring] public PlayerAbilityInfo Ability3;
        public const int SlotCount = 4;

        public PlayerAbilityInfo GetAt(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return AttackAbility;
                case 1:
                    return Ability1;
                case 2:
                    return Ability2;
                case 3:
                    return Ability3;
                default:
                    return default;
            }
        }
        public void ResetAt(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    AttackAbility.Reset();
                    break;
                case 1:
                    Ability1.Reset();
                    break;
                case 2:
                    Ability2.Reset();
                    break;
                case 3:
                    Ability3.Reset();
                    break;
                default:
                    return;
            }
        }

        public bool Contains(Entity abilityEntity)
        {
            if (abilityEntity == Entity.Null)
            {
                return false;
            }

            if (AttackAbility.Ability == abilityEntity 
                || Ability1.Ability == abilityEntity 
                || Ability2.Ability == abilityEntity 
                || Ability3.Ability == abilityEntity)
            {
                return true;
            }

            return false;
        }

        public void Reset()
        {
            AttackAbility.Reset();
            Ability1.Reset();
            Ability2.Reset();
            Ability3.Reset();
        }
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
