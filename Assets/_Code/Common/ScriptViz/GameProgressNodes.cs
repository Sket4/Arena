using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using UnityEngine;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;

namespace Arena.ScriptViz
{
    public struct GameProgressSocketData
    {
        public Entity ProgressEntity;
        public IntPtr FlagsPointer;
        public ushort FlagsCount;
    }
    
    [Serializable]
    public class GameProgressSocket : StructDataSocketBaseGeneric<GameProgressSocketData>
    {
        public GameProgressSocket()
        {
        }

        public GameProgressSocket(GameProgressSocketData defaultValue)
        {
            Value = defaultValue;
        }
    }
    
    struct GameProgressFlagCheckCommand : IScriptVizCommand
    {
        public InputVar<GameProgressSocketData> Progress;
        public int Flag;
        public Address HasFlagCommandAddress;
        public Address NoFlagCommandAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (GameProgressFlagCheckCommand*)commandData;
            
            var progress = data->Progress.Read(ref context);

            var flags = (CharacterGameProgressFlags*)progress.FlagsPointer;
            bool hasFlag = false;
            
            for (int i = 0; i < progress.FlagsCount; i++)
            {
                if (flags[i].Value == data->Flag)
                {
                    hasFlag = true;
                    break;
                }
            }

            if (hasFlag)
            {
                if (data->HasFlagCommandAddress.IsValid)
                {
                    Extensions.ExecuteCode(ref context, data->HasFlagCommandAddress);    
                }
            }
            else
            {
                if (data->NoFlagCommandAddress.IsValid)
                {
                    Extensions.ExecuteCode(ref context, data->NoFlagCommandAddress);    
                }
            }
        }
    }

    [Serializable]
    [FriendlyName("Проверить наличие флага")]
    public class GameProgressFlagCheckNode : Node, ICommandNode, IPostWriteCommandNode
    {
        [HideInInspector] public NodeInputSocket InputSocket = new NodeInputSocket();
        
        public GameProgressFlagKey FlagKey;
        
        [HideInInspector]
        public GameProgressSocket ProgressSocket = new();
        [HideInInspector]
        public NodeOutputSocket HasFlagSocket = new();
        [HideInInspector]
        public NodeOutputSocket NoFlagSocket = new();
        
        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GameProgressFlagCheckCommand();

            cmd.Flag = FlagKey != null ? FlagKey.Id : 0;
            
            compilerAllocator.InitializeInputVar(ref cmd.Progress, ProgressSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            sockets.Add(new SocketInfo(InputSocket, SocketType.In, "In"));
            sockets.Add(new SocketInfo(ProgressSocket, SocketType.In, "Прогресс"));
            sockets.Add(new SocketInfo(HasFlagSocket, SocketType.Out, "Флаг есть"));
            sockets.Add(new SocketInfo(NoFlagSocket, SocketType.Out, "Флага нет"));
        }

        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            if (FlagKey)
            {
                return $"Флаг {FlagKey.name} есть?";
            }
            else
            {
                return "Проверить наличие флага";    
            }
        }

        public override bool ShowEditableProperties => true;
        
        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<GameProgressFlagCheckCommand>(currentCommandAddress);
            cmd.HasFlagCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(HasFlagSocket);
            cmd.NoFlagCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(NoFlagSocket);
        }
    }

    public struct AddGameProgressFlagRequest : IComponentData
    {
        public int FlagKey;
    }

    struct AddGameProgressFlagCommand : IScriptVizCommand
    {
        public int FlagKey;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (AddGameProgressFlagCommand*)commandData;

            var entityRequest = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, entityRequest, new AddGameProgressFlagRequest
            {
                FlagKey = data->FlagKey
            });
        }
    }
    
    [Serializable]
    [FriendlyName("Добавить флаг прогресса")]
    public class AddGameProgressFlagCommandNode : CommandNode
    {
        public GameProgressFlagKey FlagKey;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new AddGameProgressFlagCommand();
            cmd.FlagKey = FlagKey ? FlagKey.Id : 0;
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            if (FlagKey)
            {
                return $"Добавить флаг {FlagKey.name}";
            }
            else
            {
                return "Добавить флаг прогресса";    
            }
        }
    }

    public struct GetGameProgressRequest : IComponentData
    {
        public Entity ScriptVizEntity;
        public Address CommandAddress;
        public Address DataAddress;
    }
    
    [BurstCompile]
    public struct GetGameProgressCommand : IScriptVizCommand
    {
        public Address OnDataLoadedCommandAddress;
        public Address DataAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (GetGameProgressCommand*)commandData;
            
            if (data->OnDataLoadedCommandAddress.IsInvalid)
            {
                return;
            }
            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            var request = new GetGameProgressRequest
            {
                CommandAddress = data->OnDataLoadedCommandAddress,
                DataAddress = data->DataAddress,
                ScriptVizEntity = context.OwnerEntity
            };
            context.Commands.AddComponent(context.SortIndex, requestEntity, request);
        }
    }
    
    [Serializable]
    [FriendlyName("Загрузить прогресс персонажа")]
    public class GetGameProgressCommandNode : Node, ICommandNode, IPostWriteCommandNode
    {
        public NodeInputSocket InputSocket = new();
        public NodeOutputSocket OnDataLoadedSocket = new();
        public GameProgressSocket ProgressSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            sockets.Add(new SocketInfo(InputSocket, SocketType.In));
            sockets.Add(new SocketInfo(OnDataLoadedSocket, SocketType.Out, "Прогресс загружен (delay!)"));
            sockets.Add(new SocketInfo(ProgressSocket, SocketType.Out, "Прогресс"));
        }
        
        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            return "Загрузить прогресс персонажа";
        }

        public override bool ShowEditableProperties => false;
        
        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GetGameProgressCommand();
            cmd.OnDataLoadedCommandAddress = compilerAllocator.GetSocketAddress(ProgressSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<GetGameProgressCommand>(currentCommandAddress);
            cmd.OnDataLoadedCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(OnDataLoadedSocket);
        }
    }

    public struct SetBaseLocationRequest : IComponentData
    {
        public int LocationID;
        public int SpawnPointID;
    }

    [BurstCompile]
    struct SetBaseLocationCommand : IScriptVizCommand
    {
        public int LocationID;
        public int SpawnPointID;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (SetBaseLocationCommand*)commandData;
           
            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            var request = new SetBaseLocationRequest
            {
                SpawnPointID = data->SpawnPointID,
                LocationID = data->LocationID
            };
            context.Commands.AddComponent(context.SortIndex, requestEntity, request);
        }
    }
    
    [Serializable]
    [FriendlyName("Установить базовую локацию")]
    public class SetBaseLocationNode : CommandNode
    {
        public GameSceneKey GameSceneKey;
        public SpawnPointID SpawnPointID;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetBaseLocationCommand
            {
                LocationID = GameSceneKey ? GameSceneKey.Id : -1,
                SpawnPointID = SpawnPointID ? SpawnPointID.Id : 0
            };
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            if (GameSceneKey)
            {
                return $"Установить локацию {GameSceneKey.name} как базовую";
            }

            return "Установить базовую локацию";
        }
    }
}
