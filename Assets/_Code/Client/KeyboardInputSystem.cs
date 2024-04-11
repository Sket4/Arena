using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using TzarGames.GameCore.Abilities;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(CharacterControlSystem))]
    public partial class KeyboardInputSystem : SystemBase
    {
        InputActions inputActions;
        InputActions.PlayerActions playerActions;

#if UNITY_EDITOR
        bool rightMouseButtonWasPressed;
#endif

        protected override void OnCreate()
        {
            base.OnCreate();

            inputActions = new InputActions();
            inputActions.Enable();
            playerActions = inputActions.Player;
        }

        protected override void OnUpdate()
        {
            var move = playerActions.Move.ReadValue<Vector2>();
            var view = playerActions.View.ReadValue<Vector2>();
            var attack = playerActions.Attack.IsPressed();
            var ability1 = playerActions.Ability1.IsPressed();
            var ability2 = playerActions.Ability2.IsPressed();
            var ability3 = playerActions.Ability3.IsPressed();

#if UNITY_EDITOR
            var rightMouseDown = attack;
#endif

            Entities.ForEach((Entity entity, DynamicBuffer<AbilityElement> abilities, DynamicBuffer<PendingAbilityID> pendingAbilities, ref PlayerInput input) =>
            {
                if (math.abs(input.Horizontal) < float.Epsilon)
                {
                    input.Horizontal = move.x;
                }
                if (math.abs(input.Vertical) < float.Epsilon)
                {
                    input.Vertical = move.y;
                }
            }).Run();

#if UNITY_EDITOR
            Entities
                .WithoutBurst()
                .ForEach((DynamicBuffer<AbilityElement> abilities, ref PlayerInput input) =>
            {
                if (rightMouseDown)
                {
                    rightMouseButtonWasPressed = true;
                    var abilityId = SystemAPI.GetComponent<AbilityID>(abilities[0].AbilityEntity);
                    input.PendingAbilityID = abilityId;
                }
                else
                {
                    if (rightMouseButtonWasPressed)
                    {
                        rightMouseButtonWasPressed = false;
                        input.PendingAbilityID = AbilityID.Null;
                    }
                }

            }).Run();
#endif

        }
    }
}