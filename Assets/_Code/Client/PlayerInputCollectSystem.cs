using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Arena.Client
{
    [System.Serializable]
    public struct ClientPlayerInputCommand : IBufferElementData
    {
        public PlayerInputCommand Command;
        public bool IsSent;

        // state
        public float Speed;
        public DistanceMove DistanceMove;
        public PhysicsCollider Collider;
        public CharacterInputs Inputs;
    }

    [System.Serializable]
    public struct ClientPlayerInputCommandCounter : IComponentData
    {
        public int CommandIndex;
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(PlayerCharacterTargetSystem))]
    [UpdateBefore(typeof(CharacterAbilityStateSystem))]
    public partial class PlayerInputCollectSystem : SystemBase
    {
        TimeSystem timeSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            timeSystem = World.GetExistingSystemManaged<TimeSystem>();
        }

        const int MaxCommandsToStore = 20;

        protected override void OnUpdate()
        {
            Entities
                .WithStructuralChanges()
                .WithChangeFilter<NetworkPlayer>()
                .WithAll<ControlledCharacter>()
                .WithNone<ClientPlayerInputCommand>()
                .ForEach((Entity entity, in NetworkPlayer player) =>
                {
                    if(player.ItsMe)
                    {
                        #if UNITY_EDITOR
                        UnityEngine.Debug.Log($"Adding client input command buffer for player entity {entity.Index}");
                        #endif
                        EntityManager.AddBuffer<ClientPlayerInputCommand>(entity);
                        EntityManager.AddComponentData(entity, new ClientPlayerInputCommandCounter());
                    }

                }).Run();

            var deltaTime = timeSystem.TimeDelta;

            Entities
                .WithoutBurst()
                .ForEach((DynamicBuffer<ClientPlayerInputCommand>  playerInputCommands, in ControlledCharacter character) =>
            {
                if (playerInputCommands.Length > MaxCommandsToStore)
                {
                    playerInputCommands.RemoveRange(0, playerInputCommands.Length - MaxCommandsToStore);
                }

                if(EntityManager.Exists(character.Entity) == false || HasComponent<CharacterInputs>(character.Entity) == false)
                {
                    playerInputCommands.Clear();
                    return;
                }

                var movement = GetComponent<CharacterInputs>(character.Entity);
                var pendingAbilities = GetBuffer<PendingAbilityID>(character.Entity);
                var target = GetComponent<Target>(character.Entity).Value;
                var targetNetId = NetworkID.Invalid;
                var viewDir = GetComponent<ViewDirection>(character.Entity);

                if(target != Entity.Null)
                {
                    if(HasComponent<NetworkID>(target))
                    {
                        targetNetId = GetComponent<NetworkID>(target);
                    }
                }

                //UnityEngine.Debug.Log($"Команда с позицией {GetComponent<Unity.Transforms.Translation>(character.Entity).Value}");
                short pendingAbilityId;

                if(pendingAbilities.Length > 0)
                {
                    pendingAbilityId = (short)pendingAbilities[0].Value.Value;
                }
                else
                {
                    pendingAbilityId = (short)AbilityID.Null.Value;
                }

                var command = new PlayerInputCommand
                {
                    Horizontal = movement.MoveVector.x,
                    Vertical = movement.MoveVector.z,
                    DeltaTime = deltaTime,
                    Jump = (byte)(movement.JumpRequested ? 1 : 0),
                    AbilityID = pendingAbilityId,
                    TargetNetID = targetNetId,
                    ViewDir = viewDir.Value
                };

                playerInputCommands.Add(new ClientPlayerInputCommand
                {
                    Command = command
                });

            }).Run();
        }
    }
}
