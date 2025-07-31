using TzarGames.GameCore;
using Unity.Entities;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.CharacterController;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(AbilitySystem))]
    public partial class CharacterControlSystem : GameSystemBase
    {
        EntityQuery netSyncedCharactersQuery;

        [BurstCompile]
        partial struct InputJob : IJobEntity
        {
            private const int SpeedStrikeAbilityID = 177;
            
            public void Execute( 
                ref DynamicBuffer<PendingAbilityID> pendingAbilities,
                in DynamicBuffer<AbilityArray> abilities,
                ref PlayerInput input, 
                ref CharacterInputs characterInputs, in ViewDirection viewDir, in Unity.Transforms.LocalTransform transform,
                in Velocity velocity, in PlayerAbilities playerAbilities)
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

                if (velocity.CachedMagnitude > 7 && input.PendingAbilityID == playerAbilities.AttackAbility.ID)
                {
                    bool hasSpeedStrike = false;
                    
                    foreach (var ability in abilities)
                    {
                        if (ability.AbilityID.Value == SpeedStrikeAbilityID)
                        {
                            hasSpeedStrike = true;
                            break;
                        }
                    }

                    if (hasSpeedStrike)
                    {
                        input.PendingAbilityID = new AbilityID(SpeedStrikeAbilityID);
                    }
                }

                pendingAbilities.Clear();
                pendingAbilities.Add(new PendingAbilityID { Value = input.PendingAbilityID });
            }
        }

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
            
            new InputJob().Schedule();
        }
    }
}
