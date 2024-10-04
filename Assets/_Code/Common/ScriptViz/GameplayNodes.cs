using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Arena.ScriptViz
{
    public struct ShowMessageRequest : IComponentData
    {
        public long LocalizedMessageID;
        public long ID;
    }
    
    [BurstCompile]
    public struct ShowMessageCommand : IScriptVizCommand
    {
        public long LocalizedMessageID;
        public long ID;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (ShowMessageCommand*)commandData;

            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            
            context.Commands.AddComponent(context.SortIndex, requestEntity, new ShowMessageRequest
            {
                LocalizedMessageID = data->LocalizedMessageID,
                ID = data->ID
            });
        }
    }

    // TODO skip on server
    [FriendlyName("Показать сообщение")]
    [Serializable]
    public class ShowMessageNode : CommandNode
    {
        public string OptionalID;
        public UnityEngine.Localization.LocalizedString Message;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new ShowMessageCommand();
            
            cmd.LocalizedMessageID = Message.TableEntryReference.KeyId;

            if (string.IsNullOrEmpty(OptionalID))
            {
                cmd.ID = cmd.LocalizedMessageID;
            }
            else
            {
                cmd.ID = TzarGames.GameCore.Message.CreateFromString(OptionalID).HashCode;
            }
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (Message != null)
            {
                return $"Показать '{Message.GetLocalizedString()}'";
            }
            else
            {
                return "Показать сообщение";
            }
        }
    }
    
    public struct HideMessageRequest : IComponentData
    {
        public long MessageID;
    }
    
    [BurstCompile]
    public struct HideMessageCommand : IScriptVizCommand
    {
        public long MessageID;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (HideMessageCommand*)commandData;

            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            
            context.Commands.AddComponent(context.SortIndex, requestEntity, new HideMessageRequest
            {
                MessageID = data->MessageID
            });
        }
    }
    
    // TODO skip on server
    [FriendlyName("Скрыть сообщение")]
    [Serializable]
    public class HideMessageNode : CommandNode
    {
        public string OptionalID;
        public UnityEngine.Localization.LocalizedString Message;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new HideMessageCommand();
            if (string.IsNullOrEmpty(OptionalID) == false)
            {
                cmd.MessageID = TzarGames.GameCore.Message.CreateFromString(OptionalID).HashCode;
            }
            else
            {
                cmd.MessageID = Message.TableEntryReference.KeyId;    
            }
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (Message != null)
            {
                try
                {
                    return $"Скрыть '{Message.GetLocalizedString()}'";
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return "Скрыть (ошибка)";
                }
            }
            else
            {
                return "Скрыть сообщение";
            }
        }
    }

    public struct GetMainCharacterRequest : IComponentData
    {
        public Entity ScriptVizEntity;
        public Address CommandAddress;
        public Address CharacterAddress;
    }

    [BurstCompile]
    public struct GetMainCharacterCommand : IScriptVizCommand
    {
        public Address OnCharacterLoadedCommandAddress;
        public Address CharacterAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (GetMainCharacterCommand*)commandData;

            if (data->OnCharacterLoadedCommandAddress.IsInvalid)
            {
                return;
            }
            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            var request = new GetMainCharacterRequest
            {
                CommandAddress = data->OnCharacterLoadedCommandAddress,
                CharacterAddress = data->CharacterAddress,
                ScriptVizEntity = context.OwnerEntity
            };
            context.Commands.AddComponent(context.SortIndex, requestEntity, request);
        }
    }

    [Serializable]
    [FriendlyName("Главный персонаж")]
    public class GetMainCharacterCommandNode : Node, ICommandNode, IPostWriteCommandNode
    {
        public NodeInputSocket InputSocket = new();
        public NodeOutputSocket OnCharacterLoadedSocket = new();
        public EntitySocket CharacterSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            sockets.Add(new SocketInfo(InputSocket, SocketType.In));
            sockets.Add(new SocketInfo(OnCharacterLoadedSocket, SocketType.Out, "Персонаж получен (delay!)"));
            sockets.Add(new SocketInfo(CharacterSocket, SocketType.Out, "Персонаж"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Получить главного персонажа";
        }

        public override bool ShowEditableProperties => false;

        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GetMainCharacterCommand();
            cmd.CharacterAddress = compilerAllocator.GetSocketAddress(CharacterSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<GetMainCharacterCommand>(currentCommandAddress);
            cmd.OnCharacterLoadedCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(OnCharacterLoadedSocket);
        }
    }
}
