using System;
using System.Collections.Generic;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arena.Dialogue
{
    public struct DialogueMessage : IComponentData
    {
        public Entity DialogueEntity;
        public Entity Player;
        public Entity Companion;
        public long LocalizedStringID;
    }
    
    public struct DialogueAnswer : IBufferElementData
    {
        public long LocalizedStringID;
        public Address AnswerAddress;
    }
    
    [BurstCompile]
    public struct ShowDialogueCommand : IScriptVizCommand
    {
        public long LocalizedMessageID;
        public Address AnswersStartAddress;
        public byte AnswerCount;
        public Address Player;
        public Address Companion;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (ShowDialogueCommand*)commandData;

            if (data->AnswersStartAddress.IsInvalid)
            {
                Debug.LogError($"answer start data is invalid for message ID: {data->LocalizedMessageID}, dialogue entity: {context.OwnerEntity.Index}");
                return;
            }
            
            var outPtr = (DialogueAnswer*)context.GetConstantDataPtr(data->AnswersStartAddress);
            var outCount = data->AnswerCount;

            var dialogueRequest = context.Commands.CreateEntity(context.SortIndex);

            var player = context.ReadEntityFromVariableData(data->Player);
            var companion = context.ReadEntityFromVariableData(data->Companion);
            
            Debug.Log($"Show dialogue command for player {player.Index} and companion {companion.Index}, dialogue entity {context.OwnerEntity.Index}, message ID: {data->LocalizedMessageID}");
            
            context.Commands.AddComponent(context.SortIndex, dialogueRequest, new DialogueMessage
            {
                Player = player,
                Companion = companion,
                DialogueEntity = context.OwnerEntity,
                LocalizedStringID = data->LocalizedMessageID
            });
            
            var answers = context.Commands.AddBuffer<DialogueAnswer>(context.SortIndex, dialogueRequest);
            
            for (int i = 0; i < outCount; i++)
            {
                var answer = outPtr[i];
                answers.Add(answer);
            }
        }
    }

    [Serializable]
    public class DialogueAnswerOutputSocket : DataLabelOutputSocket<UnityEngine.Localization.LocalizedString>
    {
        public override string ToString()
        {
            if (Value == null)
            {
                return "";
            }

            try
            {
                var loc = Value.GetLocalizedString();
                if (string.IsNullOrEmpty(loc))
                {
                    return "";
                }
                return loc.Substring(0, math.min(loc.Length, 15));
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowDialogueNode))]
    class ShowDialogueNodeDrawer : PropertyDrawer
    {
        private const float space = 10;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {    
                var messageProperty = property.FindPropertyRelative("Message");
                position.height = EditorGUI.GetPropertyHeight(messageProperty, label, messageProperty.isExpanded);
                EditorGUI.PropertyField(position, messageProperty, true);
                
                var answerSocketsProperty = property.FindPropertyRelative("AnswerOutputSockets");

                if (answerSocketsProperty.arraySize > 0)
                {
                    position.y += EditorGUI.GetPropertyHeight(messageProperty, label, messageProperty.isExpanded);
                    position.y += space;

                    for (int i = 0; i < answerSocketsProperty.arraySize; i++)
                    {
                        var child = answerSocketsProperty.GetArrayElementAtIndex(i);
                        
                        position.height = EditorGUI.GetPropertyHeight(child, label, messageProperty.isExpanded);
                        EditorGUI.PropertyField(position, child, true);
                        position.y += position.height;
                    }
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0;
            var messageProperty = property.FindPropertyRelative("Message");
            totalHeight += EditorGUI.GetPropertyHeight(messageProperty, label, messageProperty.isExpanded);

            var answerSocketsProperty = property.FindPropertyRelative("AnswerOutputSockets");

            if (answerSocketsProperty.arraySize == 0)
            {
                return totalHeight;
            }
            
            totalHeight += space;

            for (int i = 0; i < answerSocketsProperty.arraySize; i++)
            {
                var child = answerSocketsProperty.GetArrayElementAtIndex(i);
                totalHeight += EditorGUI.GetPropertyHeight(child, label, answerSocketsProperty.isExpanded);    
            }
            
            return totalHeight;
        }
    }
    
    [CustomPropertyDrawer(typeof(DialogueAnswerOutputSocket))]
    class DialogueAnswerOutputSocketDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {    
                var prop = property.FindPropertyRelative("Value");
                UnityEditor.EditorGUI.PropertyField(position, prop, new GUIContent(""), true);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative("Value");
            return EditorGUI.GetPropertyHeight(prop, label, prop.isExpanded);// + EditorGUIUtility.standardVerticalSpacing;
        }
    }
    #endif

    [BurstCompile]
    public struct StartDialogueCommand : IScriptVizCommand
    {
        public InputEntityVar Player;
        public InputEntityVar Companion;
        public Address Player_VarAddress;
        public Address Companion_VarAddress;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (StartDialogueCommand*)commandData;
            
            var player = data->Player.Read(ref context);
            var companion = data->Companion.Read(ref context);

            if (player == Entity.Null)
            {
                player = context.OwnerEntity;
            }
            if (companion == Entity.Null)
            {
                companion = context.OwnerEntity;
            }
            
            context.WriteEntityVariable(player, data->Player_VarAddress);
            context.WriteEntityVariable(companion, data->Companion_VarAddress);
        }
    }
    
    [FriendlyName("Старт диалога")]
    public class StartDialogueNode : CommandNode, IAdditionalVariableHandler
    {
        [HideInInspector] public EntitySocket Player = new();
        [HideInInspector] public EntitySocket Companion = new();
        private ID player_varId;
        private ID companion_varId;

        public override bool ShowEditableProperties => false;

        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new StartDialogueCommand();
            compilerAllocator.InitializeInputVar(ref cmd.Player, Player);
            compilerAllocator.InitializeInputVar(ref cmd.Companion, Companion);
            cmd.Player_VarAddress = compilerAllocator.GetVariableAddress(player_varId);
            cmd.Companion_VarAddress = compilerAllocator.GetVariableAddress(companion_varId);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public const string Player_varName = "dialogue_companion_1_5686DFE1-A08F-4F74-A074-6B2B3C593DB0";
        public const string Сompanion_varName = "dialogue_companion_2_5686DFE1-A08F-4F74-A074-6B2B3C593DB0";

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(Player, SocketType.In, "Игрок"));
            sockets.Add(new SocketInfo(Companion, SocketType.In, "Собеседник"));
        }

        public void AddAdditionalVariables(ScriptVizGraphPage page)
        {
            var playerVar = GetOrAddCompanionVariable(Player_varName, Player.AuthoringValue, page);
            playerVar.IsHidden = true;
            player_varId = playerVar.ID;
            var companionVar = GetOrAddCompanionVariable(Сompanion_varName, Companion.AuthoringValue, page);
            companionVar.IsHidden = true;
            companion_varId = companionVar.ID;
        }

        public static EntityVariable GetOrAddCompanionVariable(string varName, GameObject authoringValue, ScriptVizGraphPage page)
        {
            var variable = page.GetVariableWithName(varName);
            
            if (variable == null)
            {
                variable = new EntityVariable(varName);
                page.AddVariable(variable);
            }

            if (authoringValue != null)
            {
                (variable as EntityVariable).Value = authoringValue;    
            }
            
            return variable as EntityVariable;
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Старт диалога";
        }
    }
    
    [Serializable]
    [FriendlyName("Диалог")]
    public class ShowDialogueNode : Node, ICommandNode, IPostWriteCommandNode, IDynamicOutSocketsNode, IAdditionalVariableHandler
    {
        public UnityEngine.Localization.LocalizedString Message;
        [HideInInspector] public NodeInputSocket InputSocket = new();
        public List<DialogueAnswerOutputSocket> AnswerOutputSockets = new();
        private ID player_varId;
        private ID companion_varId;
        
        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            sockets.Add((new SocketInfo(InputSocket, SocketType.In, "In")));

            foreach(var caseVal in AnswerOutputSockets)
            {
                sockets.Add(createSocketInfo(caseVal));
            }
        }

        SocketInfo createSocketInfo(NodeOutputSocket caseSocket)
        {
            return new SocketInfo(caseSocket, SocketType.Out, "", SocketInfoFlags.AddRemoveButton | SocketInfoFlags.ShowEditorForOutputDataSocket);
        }

        public override bool ShowEditableProperties => true;

        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            ShowDialogueCommand cmd = default;
            cmd.LocalizedMessageID = Message.TableEntryReference.KeyId;
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public unsafe void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<ShowDialogueCommand>(currentCommandAddress);

            var outputList = new NativeList<DialogueAnswer>(AnswerOutputSockets.Count, Allocator.Temp);

            foreach (var element in AnswerOutputSockets)
            {
                if (element.Value == null)
                {
                    continue;
                }
                
                var nodeAddr = compilerAllocator.GetFirstConnectedNodeAddress(element);
                
                outputList.Add(new DialogueAnswer
                {
                    AnswerAddress = nodeAddr,
                    LocalizedStringID = element.Value.TableEntryReference.KeyId
                });
            }
            if(outputList.Length == 0)
            {
                cmd.AnswersStartAddress = Address.Invalid;
                cmd.AnswerCount = 0;
                return;
            }

            cmd.Player = compilerAllocator.GetVariableAddress(player_varId);
            cmd.Companion = compilerAllocator.GetVariableAddress(companion_varId);
            
            cmd.AnswerCount = (byte)outputList.Length;
            var answerSize = UnsafeUtility.SizeOf<DialogueAnswer>();
            cmd.AnswersStartAddress = compilerAllocator.WriteConstantDataAndGetAddress(outputList.GetUnsafeReadOnlyPtr(), outputList.Length * answerSize);
        }

        public SocketInfo CreateOutSocket()
        {
            var socket = new DialogueAnswerOutputSocket();
            AnswerOutputSockets.Add(socket);
            return createSocketInfo(socket);
        }

        public void RemoveOutSocket(Socket socket)
        {
            var s = socket as DialogueAnswerOutputSocket;
            if(s == null)
            {
                return;
            }
            AnswerOutputSockets.Remove(s);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if(Message == null)
                return "Диалог";

            var localized = Message.GetLocalizedString();
            if (string.IsNullOrEmpty(localized))
            {
                return "Диалог";
            }
            return localized.Substring(0, math.min(15, localized.Length));
        }

        public override float MinimumEditablePropertiesWidth => 400;
        
        public void AddAdditionalVariables(ScriptVizGraphPage page)
        {
            player_varId = StartDialogueNode.GetOrAddCompanionVariable(StartDialogueNode.Player_varName, null, page).ID;
            companion_varId = StartDialogueNode.GetOrAddCompanionVariable(StartDialogueNode.Сompanion_varName, null, page).ID;
        }
    }
}
