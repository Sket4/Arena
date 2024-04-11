using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.MatchFramework;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class PlayerInputSendSystem : SystemBase, IRpcProcessor, IInputSyncSystem
    {
        public NetworkIdentity NetIdentity { get; set; }

        public void SendInputToServer(byte[] inputData, NetMessageInfo messageInfo)
        {
        }

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((DynamicBuffer<ClientPlayerInputCommand> playerInputCommands, ref ClientPlayerInputCommandCounter commandCounter, in ControlledCharacter character, in NetworkPlayer player) =>
            {
                if(player.ItsMe == false)
                {
                    return;
                }

                if(EntityManager.Exists(character.Entity) == false || SystemAPI.HasComponent<LocalTransform>(character.Entity) == false)
                {
                    return;
                }

                if(playerInputCommands.Length == 0)
                {
                    return;
                }
                
                var transform = SystemAPI.GetComponent<LocalTransform>(character.Entity);
                var latestCommand = playerInputCommands[playerInputCommands.Length - 1];
                latestCommand.Command.Position = transform.Position;
                latestCommand.Command.TargetPosition = GetComponent<TargetPosition>(character.Entity).Value;
                latestCommand.Command.Index = commandCounter.CommandIndex;
                latestCommand.IsSent = true;
                playerInputCommands[playerInputCommands.Length - 1] = latestCommand;

                commandCounter.CommandIndex++;

                // if(latestCommand.Command.Horizontal != 0 || latestCommand.Command.Vertical != 0 || latestCommand.Command.AbilityID != AbilityID.Null.Value)
                // {
                //     UnityEngine.Debug.Log($"Посылаю команду {latestCommand.Command.Index} c позицией {latestCommand.Command.Position}, дельта {latestCommand.Command.DeltaTime}, умение: {latestCommand.Command.AbilityID}, персонаж: {character.Entity} World: {World.Name}");
                // }
                
                var commands = playerInputCommands.AsNativeArray();
                
                var commandStartIndex = math.max(playerInputCommands.Length - PlayerInputClientData.MaxCommandsToSend, 0);
                var commandsCount = playerInputCommands.Length - commandStartIndex;

                var sharedCommands = new PlayerInputCommand[commandsCount];

                for(int i=commandStartIndex, j=0; i<commandStartIndex + commandsCount; i++, j++)
                {
                    sharedCommands[j] = commands[i].Command;
                }
                
                var clientInputData = new PlayerInputClientData
                {
                    Commands = sharedCommands
                };

                // if (math.abs(latestCommand.Command.Horizontal) > float.Epsilon)
                // {
                //     UnityEngine.Debug.Log($"Cli Cmd {latestCommand.Command.Index}, target: {latestCommand.Command.TargetNetID.ID}, ({latestCommand.Command.Horizontal}, {latestCommand.Command.Vertical}), dt: {latestCommand.Command.DeltaTime} pos: {translation.Value}");
                // }

                this.RPC(SendInputToServer, clientInputData.ToByteArray());

            }).Run();
        }
    }
}
