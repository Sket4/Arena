using TzarGames.GameCore;
using Unity.Entities;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.CharacterController;
using Unity.Mathematics;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(AbilitySystem))]
    public partial class CharacterControlSystem : GameSystemBase
    {
        EntityQuery netSyncedCharactersQuery;

        protected override void OnSystemUpdate()
        {
            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref netSyncedCharactersQuery)
                .WithAll<CharacterControllerComponent>()
                .ForEach((Entity entity, in DynamicBuffer<NetworkMovements> netMoves) =>
                {
                    if(netMoves.Length > 0)
                    {
                        Debug.Log($"Disabling character component from net synced entity {entity.Index}");
                        EntityManager.SetComponentEnabled<CharacterControllerComponent>(entity, false);
                    }

                }).Run();

            Entities.ForEach((Entity entity, DynamicBuffer<PendingAbilityID> pendingAbilities, ref PlayerInput input, ref CharacterInputs characterInputs, in ViewDirection viewDir, in Unity.Transforms.LocalTransform transform) =>
            {
                var inputVector = new Vector3(input.Horizontal, 0, input.Vertical);
                var dir = viewDir.Value;
                dir.y = 0;

                dir = math.normalizesafe(dir, math.forward(transform.Rotation   ));

                var rot = Quaternion.LookRotation(dir, Vector3.up);
                inputVector = rot * inputVector;

                characterInputs.MoveVector = inputVector;
                input.Horizontal = 0;
                input.Vertical = 0;


                pendingAbilities.Clear();
                pendingAbilities.Add(new PendingAbilityID { Value = input.PendingAbilityID });

            }).Run();
        }
    }
}
