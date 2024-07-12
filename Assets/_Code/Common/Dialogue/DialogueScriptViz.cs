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

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (ShowDialogueCommand*)commandData;

            if (data->AnswersStartAddress.IsInvalid)
            {
                return;
            }

            var outPtr = (DialogueAnswer*)context.GetConstantDataPtr(data->AnswersStartAddress);
            var outCount = data->AnswerCount;

            var dialogueRequest = context.Commands.CreateEntity(context.SortIndex);
            
            context.Commands.AddComponent(context.SortIndex, dialogueRequest, new DialogueMessage
            {
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
    
    [UnityEditor.CustomPropertyDrawer(typeof(DialogueAnswerOutputSocket))]
    class DialogueAnswerOutputSocketDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.EditorGUI.BeginProperty(position, label, property);
            {    
                var prop = property.FindPropertyRelative("Value");
                UnityEditor.EditorGUI.PropertyField(position, prop, new GUIContent(""), true);
            }
            UnityEditor.EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative("Value");
            return EditorGUI.GetPropertyHeight(prop, label, prop.isExpanded);// + EditorGUIUtility.standardVerticalSpacing;
        }
    }
    #endif
    
    [Serializable]
    [FriendlyName("Диалог")]
    public class ShowDialogueNode : Node, ICommandNode, IPostWriteCommandNode, IDynamicOutSocketsNode, ICustomNodeName
    {
        public UnityEngine.Localization.LocalizedString Message;
        [HideInInspector] public NodeInputSocket InputSocket = new();
        public List<DialogueAnswerOutputSocket> AnswerOutputSockets = new();

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
                var nodeAddr = compilerAllocator.GetFirstConnectedNodeAddress(element);
                if(nodeAddr.IsInvalid)
                {
                    continue;
                }

                if (element.Value == null)
                {
                    continue;
                }
                
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

        public string GetNodeName()
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

        public override float MinimumEditablePropertiesWidth => 350;
    }
}
