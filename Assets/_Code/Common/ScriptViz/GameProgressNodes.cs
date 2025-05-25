using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Arena.Quests;
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
        public PointerWrapper FlagsPointer;
        public ushort FlagsCount;
        public PointerWrapper KeysPointer;
        public ushort KeysCount;
        public PointerWrapper QuestsPointer;
        public ushort QuestCount;
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
    
    [BurstCompile]
    struct QuestActiveCheckCommand : IScriptVizCommand
    {
        public InputVar<GameProgressSocketData> Progress;
        public int QuestID;
        public Address QuestActiveCommandAddress;
        public Address QuestCompleteCommandAddress;
        public Address QuestFailedCommandAddress;
        public Address QuestNotAddedCommandAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (QuestActiveCheckCommand*)commandData;
            
            var progress = data->Progress.Read(ref context);

            var quests = (CharacterGameProgressQuests*)progress.QuestsPointer.Value;
            bool hasQuest = false;
            QuestState state = QuestState.Failed;
            
            for (int i = 0; i < progress.QuestCount; i++)
            {
                if (quests[i].QuestID == data->QuestID)
                {
                    hasQuest = true;
                    state = quests[i].QuestState;
                    break;
                }
            }

            if (hasQuest)
            {
                switch (state)
                {
                    case QuestState.Active:
                        if (data->QuestActiveCommandAddress.IsValid)
                        {
                            Extensions.ExecuteCode(ref context, data->QuestActiveCommandAddress);    
                        }
                        break;
                    case QuestState.Completed:
                        if (data->QuestCompleteCommandAddress.IsValid)
                        {
                            Extensions.ExecuteCode(ref context, data->QuestCompleteCommandAddress);    
                        }
                        break;
                    case QuestState.Failed:
                        if (data->QuestFailedCommandAddress.IsValid)
                        {
                            Extensions.ExecuteCode(ref context, data->QuestFailedCommandAddress);    
                        }
                        break;
                    default:
                        Debug.LogError("unknown state");
                        break;
                }
            }
            else
            {
                if (data->QuestNotAddedCommandAddress.IsValid)
                {
                    Extensions.ExecuteCode(ref context, data->QuestNotAddedCommandAddress);    
                }
            }
        }
    }
    
    [Serializable]
    [FriendlyName("Получить статус квеста")]
    public class GameProgressQuestCheckNode : Node, ICommandNode, IPostWriteCommandNode
    {
        [HideInInspector] public NodeInputSocket InputSocket = new NodeInputSocket();
        
        public QuestKey questKey;
        
        [HideInInspector]
        public GameProgressSocket ProgressSocket = new();
        [HideInInspector]
        public NodeOutputSocket QuestActiveSocket = new();
        [HideInInspector]
        public NodeOutputSocket QuestCompletedSocket = new();
        [HideInInspector]
        public NodeOutputSocket QuestFailedSocket = new();
        [HideInInspector]
        public NodeOutputSocket NoQuestSocket = new();
        
        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new QuestActiveCheckCommand();

            cmd.QuestID = questKey ? questKey.Id : 0;
            
            compilerAllocator.InitializeInputVar(ref cmd.Progress, ProgressSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            sockets.Add(new SocketInfo(InputSocket, SocketType.In, "In"));
            sockets.Add(new SocketInfo(ProgressSocket, SocketType.In, "Прогресс"));
            sockets.Add(new SocketInfo(QuestActiveSocket, SocketType.Out, "Квест активен"));
            sockets.Add(new SocketInfo(QuestCompletedSocket, SocketType.Out, "Квест закончен"));
            sockets.Add(new SocketInfo(QuestFailedSocket, SocketType.Out, "Квест провален"));
            sockets.Add(new SocketInfo(NoQuestSocket, SocketType.Out, "Квеста нет"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (questKey)
            {
                return $"Статус квеста {questKey.name}";
            }
            else
            {
                return "Получить статус квеста";    
            }
        }

        public override bool ShowEditableProperties => true;
        
        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<QuestActiveCheckCommand>(currentCommandAddress);
            cmd.QuestActiveCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(QuestActiveSocket);
            cmd.QuestCompleteCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(QuestCompletedSocket);
            cmd.QuestFailedCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(QuestFailedSocket);
            cmd.QuestNotAddedCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(NoQuestSocket);
        }
    }
    
    [BurstCompile]
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

            var flags = (CharacterGameProgressFlags*)progress.FlagsPointer.Value;
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

            cmd.Flag = FlagKey ? FlagKey.Id : 0;
            
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

        public override string GetNodeName(ScriptVizGraphPage page)
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
    
    public struct AddGameProgressQuestRequest : IComponentData
    {
        public int QuestKey;
        public QuestState State;
    }

    [BurstCompile]
    struct SetGameProgressQuestCommand : IScriptVizCommand
    {
        public int QuestKey;
        public QuestState State;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (SetGameProgressQuestCommand*)commandData;

            var entityRequest = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, entityRequest, new AddGameProgressQuestRequest
            {
                QuestKey = data->QuestKey,
                State = data->State
            });
        }
    }
    
    [Serializable]
    [FriendlyName("Добавить / установить квест")]
    public class AddGameProgressQuestCommandNode : CommandNode
    {
        public QuestKey questKey;
        public QuestState State;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetGameProgressQuestCommand();
            cmd.QuestKey = questKey ? questKey.Id : 0;
            cmd.State = State;
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (questKey)
            {
                switch (State)
                {
                    case QuestState.Active:
                        return $"Добавить квест {questKey.name}";
                    case QuestState.Completed:
                        return $"Закончить квест {questKey.name}";
                    case QuestState.Failed:
                        return $"Провалить квест {questKey.name}";
                    default:
                        return $"ошибка";
                }
            }
            else
            {
                return "Добавить / установить квест";    
            }
        }
    }


    public struct AddGameProgressFlagRequest : IComponentData
    {
        public int FlagKey;
    }

    [BurstCompile]
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

        public override string GetNodeName(ScriptVizGraphPage page)
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
    
    public struct SaveGameRequest : IComponentData
    {
    }
    
    [BurstCompile]
    struct SaveGameRequestCommand : IScriptVizCommand
    {
        private byte fakeValue;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var entityRequest = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, entityRequest, new SaveGameRequest());
        }
    }
    
    public struct SetGameProgressKeyRequest : IComponentData
    {
        public int Key;
        public int Value;
    }
    
    [BurstCompile]
    struct SetGameProgressKeyCommand : IScriptVizCommand
    {
        public int Key;
        public InputVar<int> Value;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (SetGameProgressKeyCommand*)commandData;
            var val = data->Value.Read(ref context);
            var entityRequest = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, entityRequest, new SetGameProgressKeyRequest
            {
                Key = data->Key,
                Value = val
            });
        }
    }
    
    [Serializable]
    [FriendlyName("Сохранить прогресс")]
    public class SaveGameRequestCommandNode : CommandNode
    {
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SaveGameRequestCommand();
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Сохранить прогресс";
        }
    }
    
    [Serializable]
    [FriendlyName("Записать ключ прогресса")]
    public class SetGameProgressKeyCommandNode : CommandNode
    {
        public GameProgressIntKey Key;
        [HideInInspector] public IntSocket ValueSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(ValueSocket, SocketType.In, "Значение"));
        }

        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetGameProgressKeyCommand();
            cmd.Key = Key ? Key.Id : 0;
            compilerAllocator.InitializeInputVar(ref cmd.Value, ValueSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (Key)
            {
                return $"Записать ключ {Key.name}";
            }
            else
            {
                return "Записать ключ прогресса";    
            }
        }
    }
    
    [BurstCompile]
    struct GetProgressKeyValueCommand : IScriptVizCommand
    {
        public InputVar<GameProgressSocketData> Progress;
        public int Key;
        public Address ValueOutputAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (GetProgressKeyValueCommand*)commandData;

            if (data->ValueOutputAddress.IsInvalid)
            {
                Debug.LogError("out address not connected");
                return;
            }
            
            var progress = data->Progress.Read(ref context);

            var keys = (CharacterGameProgressKeyValue*)progress.KeysPointer.Value;
            int val = 0;
            
            for (int i = 0; i < progress.KeysCount; i++)
            {
                if (keys[i].Key == data->Key)
                {
                    val = keys[i].Value;
                    break;
                }
            }

            Debug.Log($"get game progress key {data->Key} value {val}");
            context.WriteToTemp(ref val, data->ValueOutputAddress);
        }
    }
    
    [Serializable]
    [FriendlyName("Получить значение ключа прогресса")]
    public class GetGameProgressKeyValueNode : CommandNode, IPostWriteCommandNode
    {
        public GameProgressIntKey Key;
        
        [HideInInspector]
        public GameProgressSocket ProgressSocket = new();

        [HideInInspector] public IntSocket ValueSocket = new();
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GetProgressKeyValueCommand();

            cmd.Key = Key != null ? Key.Id : 0;
            
            compilerAllocator.InitializeInputVar(ref cmd.Progress, ProgressSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(ProgressSocket, SocketType.In, "Прогресс"));
            sockets.Add(new SocketInfo(ValueSocket, SocketType.Out, "Значение"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (Key)
            {
                return $"Получить значение ключа {Key.name}";
            }
            else
            {
                return "Получить значение ключа";    
            }
        }

        public override bool ShowEditableProperties => true;
        
        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<GetProgressKeyValueCommand>(currentCommandAddress);
            cmd.ValueOutputAddress = compilerAllocator.GetSocketAddress(ValueSocket);
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
        
        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Загрузить прогресс персонажа";
        }

        public override bool ShowEditableProperties => false;
        
        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GetGameProgressCommand();
            cmd.DataAddress = compilerAllocator.GetSocketAddress(ProgressSocket);
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

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (GameSceneKey)
            {
                return $"Установить локацию {GameSceneKey.name} как базовую";
            }

            return "Установить базовую локацию";
        }
    }

    public struct StartQuestRequest : IComponentData
    {
        public int QuestID;
    }
    
    [BurstCompile]
    struct StartQuestCommand : IScriptVizCommand
    {
        public int QuestID;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (StartQuestCommand*)commandData;
           
            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            var request = new StartQuestRequest
            {
                QuestID = data->QuestID
            };
            context.Commands.AddComponent(context.SortIndex, requestEntity, request);
            
            Debug.Log($"Sending start quest request, quest id: {data->QuestID}");
        }
    }

    [Serializable]
    [FriendlyName("Начать задание")]
    public class StartQuestNode : CommandNode
    {
        public QuestKey Key;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new StartQuestCommand
            {
                QuestID = Key ? Key.Id : -1
            };
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (Key)
            {
                return $"Начать задание {Key.name}";
            }
            return "Начать задание (не назначено)";
        }
    }
}
