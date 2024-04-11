using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MultiplayerKit;
using Unity.Collections;
using Unity.Entities;

namespace Arena.Server
{
    struct PlayerInputInfo : IComponentData
    {
        public int LastCommandIndex;
        public double LastCommandApplyTime;
    }

    struct ServerPlayerInputCommand : IBufferElementData, IComparable<ServerPlayerInputCommand>
    {
        public PlayerInputCommand ClientCommand;
        public bool IsProcessed;

        public int CompareTo(ServerPlayerInputCommand other)
        {
            return ClientCommand.CompareTo(other.ClientCommand);
        }
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(TimeSystem))]
    [UpdateBefore(typeof(CharacterAbilityStateSystem))]
    public partial class PlayerInputReceiveSystem : SystemBase, IRpcProcessor, IInputSyncSystem
    {
        public NetworkIdentity NetIdentity { get; set; }
        const int maxCommandCount = 30;
        ServerSystem serverSystem;
        EntityQuery netIdsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            serverSystem = World.GetExistingSystemManaged<ServerSystem>();
            netIdsQuery = World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkID>());
        }

        protected override void OnUpdate()
        {
            var currentTime = serverSystem.NetTime;
            var commandBuffers = GetBufferLookup<ServerPlayerInputCommand>();
            var netIdChunks = netIdsQuery.ToArchetypeChunkArray(Allocator.Temp);
            var entityType = World.EntityManager.GetEntityTypeHandle();
            var netIdType = World.EntityManager.GetComponentTypeHandle<NetworkID>(true);

            // применяем самую раннюю команду ввода для каждого из игроков
            Entities
                .WithDisposeOnCompletion(netIdChunks)
                .WithReadOnly(netIdChunks)
                .ForEach((Entity playerCharacterEntity,
                DynamicBuffer<PendingAbilityID> pendingAbilities,
                ref CharacterInputs movement,
                ref ViewDirection viewDirection,
                ref TargetPosition targetPosition,
                ref DeltaTime deltaTime,
                ref Target target,
                in PlayerController playerController) =>
            {
                if (commandBuffers.HasComponent(playerController.Value) == false)
                {
                    return;
                }

                var commands = commandBuffers[playerController.Value];

                if (commands.Length == 0)
                {
                    deltaTime.Value = 0;
                    return;
                }

                if (commands.Length >= 30)
                {
                    UnityEngine.Debug.LogError("Too many commands");
                    commands.RemoveRange(0, commands.Length - 1);
                }

                var serverCommand = commands[0];
                var command = serverCommand.ClientCommand;

                serverCommand.IsProcessed = true;
                commands[0] = serverCommand;

                SetComponent(playerController.Value,
                    new PlayerInputInfo
                    {
                        LastCommandIndex = command.Index,
                        LastCommandApplyTime = currentTime
                    });

                // movement
                movement.MoveVector.x = command.Horizontal;
                movement.MoveVector.z = command.Vertical;
                movement.JumpRequested = command.Jump > 0;

                // view dir
                viewDirection.Value = command.ViewDir;

                // target position
                targetPosition.Value = command.TargetPosition;

                // abilities
                pendingAbilities.Clear();
                pendingAbilities.Add(new PendingAbilityID { Value = new AbilityID(command.AbilityID) });

                // target
                if (command.TargetNetID != NetworkID.Invalid)
                {
                    var targetEntity = Entity.Null;

                    foreach (var netChunk in netIdChunks)
                    {
                        var entities = netChunk.GetNativeArray(entityType);
                        var netIds = netChunk.GetNativeArray(netIdType);

                        for (int i = 0; i < netIds.Length; i++)
                        {
                            var netId = netIds[i];

                            if (netId == command.TargetNetID)
                            {
                                targetEntity = entities[i];
                                break;
                            }
                        }

                        if (targetEntity != Entity.Null)
                        {
                            break;
                        }
                    }

                    target.Value = targetEntity;
                }
                else
                {
                    target.Value = Entity.Null;
                }

                // delta time
                deltaTime.Value = command.DeltaTime;

                // if(Unity.Mathematics.math.length(movement.MoveVector) > float.Epsilon || command.AbilityID != AbilityID.Null.Value)
                // {
                //     UnityEngine.Debug.Log($"Ser Cmd {command.Index}, target: {command.TargetNetID.ID}, ({command.Horizontal}, {command.Vertical}), dt: {command.DeltaTime}, abil: {command.AbilityID}");
                // }

            }).Run();
        }

        public void SendInputToServer(byte[] inputData, NetMessageInfo messageInfo)
        {
            if (PlayerInputClientData.FromByteArray(inputData, out PlayerInputClientData playerInput) == false)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError("Не удалось преобразовать массив байт в PlayerInputClientData");
#endif
                return;
            }

            DynamicBuffer<ServerPlayerInputCommand> commands;
            PlayerInputInfo playerInputInfo;

            if (HasComponent<PlayerInputInfo>(messageInfo.SenderEntity))
            {
                playerInputInfo = GetComponent<PlayerInputInfo>(messageInfo.SenderEntity);
            }
            else
            {
                playerInputInfo = new PlayerInputInfo
                {
                    LastCommandIndex = -1
                };
                EntityManager.AddComponentData(messageInfo.SenderEntity, playerInputInfo);
            }

            if (EntityManager.HasComponent<ServerPlayerInputCommand>(messageInfo.SenderEntity) == false)
            {
                commands = EntityManager.AddBuffer<ServerPlayerInputCommand>(messageInfo.SenderEntity);
            }
            else
            {
                commands = GetBuffer<ServerPlayerInputCommand>(messageInfo.SenderEntity);
            }

            foreach (var receivedCommand in playerInput.Commands)
            {
                if (receivedCommand.Index <= playerInputInfo.LastCommandIndex)
                {
                    continue;
                }

                bool alreadyAdded = false;
                for (int i = 0; i < commands.Length; i++)
                {
                    var command = commands[i];

                    if (command.ClientCommand.Index == receivedCommand.Index)
                    {
                        command.ClientCommand.Position = receivedCommand.Position;
                        commands[i] = command;
                        alreadyAdded = true;
                        break;
                    }
                }
                if (alreadyAdded == false)
                {
                    commands.Add(new ServerPlayerInputCommand
                    {
                        ClientCommand = receivedCommand,
                        IsProcessed = false
                    });
                }
            }

            commands.AsNativeArray().Sort();

            if (commands.Length > maxCommandCount)
            {
                //{
                //    var sbReceived = new System.Text.StringBuilder();
                //    foreach (var command in playerInput.Commands)
                //    {
                //        sbReceived.Append(command.Index);
                //        sbReceived.Append(',');
                //    }
                //    var sbTotal = new System.Text.StringBuilder();
                //    foreach (var command in commands)
                //    {
                //        sbTotal.Append(command.ClientCommand.Index);
                //        sbTotal.Append(',');
                //    }

                //    UnityEngine.Debug.LogWarning($"Max input commands limit exceeded, received inputs: {playerInput.Commands.Length} ({sbReceived}) from player {messageInfo.Sender.ID} ({messageInfo.SenderEntity}), total commands: {sbTotal}");
                //}

                //commands.RemoveRange(0, commands.Length - maxCommandCount);
                commands.RemoveRange(0, commands.Length - 1);   // оставляем только самую свежую команду
            }
        }
    }
}
