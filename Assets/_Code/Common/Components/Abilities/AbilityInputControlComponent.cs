using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Abilities
{
    [Serializable]
    public struct AbilityInputControl : IComponentData, IAbilityComponentJob<AbilityInputControlJob>
    {
        public  AbilityInputControlType ControlType;
    }

    public enum AbilityInputControlType : byte
    {
        StopOnButtonUp,
        KeepRunningOnButtonPress
    }

    [DisallowMultipleComponent]
    public class AbilityInputControlComponent : ComponentDataBehaviour<AbilityInputControl>
    {
    }

    public struct AbilityInputControlJob
    {
        [ReadOnly]
        public ComponentLookup<PlayerInput> PlayerInputFromEntity;


        [MethodPriority(AbilitySystem.DefaultLowPriority)]
        public void OnUpdate(in AbilityOwner abilityOwner, in AbilityID abilityId, in AbilityInputControl component, ref AbilityControl abilityControl)
        {
            var input = PlayerInputFromEntity[abilityOwner.Value];

            switch (component.ControlType)
            {
                case AbilityInputControlType.StopOnButtonUp:
                    if (input.PendingAbilityID != abilityId)
                    {
                        abilityControl.StopRequest = true;
                    }
                    break;
                case AbilityInputControlType.KeepRunningOnButtonPress:
                    if (input.PendingAbilityID == abilityId)
                    {
                        abilityControl.StopRequest = false;
                    }
                    break;
            }
        }
    }
}

