using Unity.CharacterController;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using NetworkPlayer = TzarGames.MultiplayerKit.NetworkPlayer;

namespace Arena.Server
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class InputCommandPostprocessSystem : SystemBase, IServerCorrectionSystem, IRpcProcessor
    {
        public NetworkIdentity NetIdentity { get; set; }

        // только для удобства вызова RPC
        public void CorrectPositionOnClient(int inputCommandIndex, CharacterContollerStateData stateData)
        {
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, DynamicBuffer<ServerPlayerInputCommand> inputCommands, in NetworkPlayer player, in ControlledCharacter controlledCharacter) =>
            {
                if(inputCommands.IsEmpty)
                {
                    return;
                }

                var serverCommand = inputCommands[0];

                if (serverCommand.IsProcessed == false)
                {
                    return;
                }

                var command = serverCommand.ClientCommand;
                inputCommands.RemoveAt(0);

                if(SystemAPI.HasComponent<LocalTransform>(controlledCharacter.Entity) && SystemAPI.HasComponent<KinematicCharacterBody>(controlledCharacter.Entity))
                {
                    var characterTransform = SystemAPI.GetComponent<LocalTransform>(controlledCharacter.Entity);
                    var characterDistanceMove = SystemAPI.GetComponent<DistanceMove>(controlledCharacter.Entity);
                    var dist = math.distance(characterTransform.Position, command.Position);

                    if (dist <= PlayerInputCommand.MaxPositionError)
                    {
                        characterTransform.Position = command.Position;
                        SystemAPI.SetComponent(controlledCharacter.Entity, characterTransform);
                    }
                    else
                    {
                        // слишком большое расхождение в позициях, надо корректировать на клиенте
                        
                        var controllerData = SystemAPI.GetComponent<KinematicCharacterBody>(controlledCharacter.Entity);
                        #if UNITY_EDITOR
                            Debug.LogWarning($"{command.Index} команда: расхождения в позициях персонажа {controlledCharacter.Entity.Index}, контролируемого игроком {player.ID}, на клиенте и сервере: {dist}, посылаем RPC для корректировки команды в позицию {characterTransform.Position}. Позиция от клиента: {command.Position}, relvel {controllerData.RelativeVelocity} ({math.length(controllerData.RelativeVelocity)}))");
                        #endif
                        
                        var data = new CharacterContollerStateData(controllerData.IsGrounded, controllerData.RelativeVelocity, characterTransform.Position, characterTransform.Rotation, characterDistanceMove);

                        this.RPC(CorrectPositionOnClient, player, command.Index, data);
                    }
                }

            }).Run();
        }
    }
}
