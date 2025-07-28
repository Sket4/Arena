using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Abilities
{
    [Serializable]
    public struct ArenaRequireItemsData : IComponentData, IAbilityComponentJob<ArenaRequireItemsValidationAbilityJob>
    {
        public bool RequireActiveShield;
    }
    
    /// <summary>
    /// Запрещает выполнение умения, если в инвентаре нет необходимых предметов
    /// </summary>
    public class ArenaRequireItemsAbilityComponent : ComponentDataBehaviour<ArenaRequireItemsData>, IComponentHelpProvider
    {
        public string GetHelpText()
        {
            return new TzarGames.GameCore.Tools.HelpFormatHelper
            {
                MainDescription = $"Запрещает выполнение умения, если в инвентаре нет необходимых предметов",
                Parameters = new []
                {
                    new TzarGames.GameCore.Tools.ParameterInfo(nameof(Value.RequireActiveShield), "Требование активированного щита для выполнения умения"),
                }

            }.ToString();
        }
    }

    [BurstCompile]
    public struct ArenaRequireItemsValidationAbilityJob
    {
        [ReadOnly] public ComponentLookup<CharacterEquipment> EquipmentLookup;

        public bool OnValidate(in AbilityOwner abilityOwner, in ArenaRequireItemsData requirements)
        {
            return CheckForRequiredItems(in abilityOwner, in requirements);
        }

        public bool CheckForRequiredItems(
            in AbilityOwner abilityOwner,
            in ArenaRequireItemsData requirements)
        {
            if (EquipmentLookup.TryGetComponent(abilityOwner.Value, out var equipment) == false)
            {
                Debug.LogError($"Owner {abilityOwner.Value.Index} does not have an CharacterEquipment component");
                return false;
            }

            if (requirements.RequireActiveShield && equipment.LeftHandShield == Entity.Null)
            {
                return false;
            }

            return true;
        }
    }
}
