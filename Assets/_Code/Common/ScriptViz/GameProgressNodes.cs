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
    [Serializable]
    public struct GameProgressLoadedEventData : IBufferElementData, ICommandAddressData
    {
        [SerializeField] private Address commandAddress;
        public Address DataAddress;
        public Address CommandAddress { get => commandAddress; set => commandAddress = value; }
    }

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
    
    [Serializable]
    [FriendlyName("Прогресс персонажа загружен")]
    public class GameProgressLoadedEventNode : DynamicBufferEventNode<GameProgressLoadedEventData>, ICustomNodeName
    {
        public GameProgressSocket Progress = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(Progress, SocketType.Out, "Прогресс"));
        }

        protected override GameProgressLoadedEventData GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var data = base.GetConvertedData(entity, baker, compiler);
            data.DataAddress = compiler.GetSocketAddress(Progress);
            return data;
        }

        public string GetNodeName()
        {
            return "Прогресс персонажа загружен";
        }

        public override bool ShowEditableProperties => false;
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
    public class GameProgressFlagCheckNode : Node, ICommandNode, ICustomNodeName, IPostWriteCommandNode
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

        public string GetNodeName()
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
    public class AddGameProgressFlagCommandNode : CommandNode, ICustomNodeName
    {
        public GameProgressFlagKey FlagKey;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new AddGameProgressFlagCommand();
            cmd.FlagKey = FlagKey ? FlagKey.Id : 0;
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public string GetNodeName()
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
}
