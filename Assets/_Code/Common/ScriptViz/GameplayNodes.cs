using System;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;

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
                return $"Скрыть '{Message.GetLocalizedString()}'";
            }
            else
            {
                return "Скрыть сообщение";
            }
        }
    }
}
