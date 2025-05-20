using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
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
        public Address MessageEntityAddress;

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

            if (data->MessageEntityAddress.IsValid)
            {
                context.WriteToTemp(requestEntity, data->MessageEntityAddress);
            }
        }
    }
    
    public struct MessageNumbers : IComponentData
    {
        public int NumberA;
        public int NumberB;
    }
    
    [BurstCompile]
    public struct SetMessageNumbersCommand : IScriptVizCommand
    {
        public InputEntityVar MessageEntity;
        public InputVar<int> NumberA;
        public InputVar<int> NumberB;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (SetMessageNumbersCommand*)commandData;

            if (data->MessageEntity.Address.IsInvalid)
            {
                return;
            }

            var messageEntity = data->MessageEntity.Read(ref context);

            if (messageEntity == Entity.Null)
            {
                Debug.LogError("failed to set message numbers - null message entity");
                return;
            }
            
            context.Commands.AddComponent(context.SortIndex, messageEntity, new MessageNumbers
            {
                NumberA = data->NumberA.Read(ref context),
                NumberB = data->NumberB.Read(ref context)
            });
        }
    }
    
    [FriendlyName("Установить числа в сообщение")]
    [Serializable]
    public class SetMessageNumbersNode : CommandNode
    {
        public EntitySocket MessageEntitySocket = new();
        public IntSocket NumberA = new();
        public IntSocket NumberB = new();

        public override bool ShowEditableProperties => false;

        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetMessageNumbersCommand();
            
            compilerAllocator.InitializeInputVar(ref cmd.MessageEntity, MessageEntitySocket);
            compilerAllocator.InitializeInputVar(ref cmd.NumberA, NumberA);
            compilerAllocator.InitializeInputVar(ref cmd.NumberB, NumberB);
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(MessageEntitySocket, SocketType.In, "Сообщение"));
            sockets.Add(new SocketInfo(NumberA, SocketType.In, "Число A"));
            sockets.Add(new SocketInfo(NumberB, SocketType.In, "Число B"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Установить числа в сообщение";
        }
    }

    // TODO skip on server
    [FriendlyName("Показать сообщение")]
    [Serializable]
    public class ShowMessageNode : CommandNode
    {
        [HideInInspector] public EntitySocket MessageEntityOutSocket = new();
        public string OptionalID;
        public UnityEngine.Localization.LocalizedString Message;

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            
            if (MessageEntityOutSocket == null)
            {
                MessageEntityOutSocket = new EntitySocket();
                MessageEntityOutSocket.ID.Regenerate();
            }
            sockets.Add(new SocketInfo(MessageEntityOutSocket, SocketType.Out, "Сущность сообщения (delay)"));
        }

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

            if (MessageEntityOutSocket != null)
            {
                cmd.MessageEntityAddress = compilerAllocator.GetSocketAddress(MessageEntityOutSocket);    
            }
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            try
            {
                if (Message != null)
                {
                    return $"Показать '{Message.GetLocalizedString().Trim()}'";
                }
                else
                {
                    return "Показать сообщение";
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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
                    try
                    {
                        return $"Скрыть '{Message.GetLocalizedString()}'";
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return "Скрыть (ошибка)";    
                    }
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
    
    [BurstCompile]
    public struct SetCharacterEyeColorCommand : IScriptVizCommand
    {
        public CharacterEyeColor Color;
        public InputEntityVar Target;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (SetCharacterEyeColorCommand*)commandData;

            var target = data->Target.Read(ref context);

            if (target == Entity.Null)
            {
                Debug.LogError("target is null");
                return;
            }
            
            context.Commands.SetComponent(context.SortIndex, target, data->Color);
        }
    }
    
    [FriendlyName("Сменить цвет глаз персонажа")]
    [Serializable]
    public class SetCharacterEyeColorNode : CommandNode
    {
        public Color Color;
        [HideInInspector]
        public EntitySocket TargetSocket = new();
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetCharacterEyeColorCommand();
            compilerAllocator.InitializeInputVar(ref cmd.Target, TargetSocket);
            var c = (Color32)Color;
            cmd.Color = new CharacterEyeColor
            {
                Value = new PackedColor(c.r, c.g, c.b, c.a)
            };
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetSocket, SocketType.In, "Персонаж"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Сменить глаз волос";
        }
    }
    
    [BurstCompile]
    public struct SetCharacterHairColorCommand : IScriptVizCommand
    {
        public CharacterHairColor Color;
        public InputEntityVar Target;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (SetCharacterHairColorCommand*)commandData;

            var target = data->Target.Read(ref context);

            if (target == Entity.Null)
            {
                Debug.LogError("target is null");
                return;
            }
            
            context.Commands.SetComponent(context.SortIndex, target, data->Color);
        }
    }
    
    [FriendlyName("Сменить цвет волос персонажа")]
    [Serializable]
    public class SetCharacterHairColorNode : CommandNode
    {
        public Color Color;
        [HideInInspector]
        public EntitySocket TargetSocket = new();
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetCharacterHairColorCommand();
            compilerAllocator.InitializeInputVar(ref cmd.Target, TargetSocket);
            var c = (Color32)Color;
            cmd.Color = new CharacterHairColor
            {
                Value = new PackedColor(c.r, c.g, c.b, c.a)
            };
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetSocket, SocketType.In, "Персонаж"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Сменить цвет волос";
        }
    }
    
    [BurstCompile]
    public struct SetCharacterHeadCommand : IScriptVizCommand
    {
        public CharacterHead Head;
        public InputEntityVar Target;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (SetCharacterHeadCommand*)commandData;

            var target = data->Target.Read(ref context);

            if (target == Entity.Null)
            {
                Debug.LogError("target head is null");
                return;
            }
            
            context.Commands.SetComponent(context.SortIndex, target, data->Head);
        }
    }
    
    [FriendlyName("Сменить голову персонажа")]
    [Serializable]
    public class SetCharacterHeadNode : CommandNode
    {
        public HeadKey HeadID;
        [HideInInspector]
        public EntitySocket TargetSocket = new();
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new SetCharacterHeadCommand();
            compilerAllocator.InitializeInputVar(ref cmd.Target, TargetSocket);
            cmd.Head = new CharacterHead
            {
                ModelID = new PrefabID(HeadID != null ? HeadID.Id : 0)
            };
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetSocket, SocketType.In, "Персонаж"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (HeadID)
            {
                return $"Установить голову '{HeadID.name}'";
            }
            else
            {
                return "Установить голову";
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
    
    public struct GetAimPointRequest : IComponentData
    {
        public Entity RequestEntity;
        public Entity TargetEntity;
        public Address AimPointAddress;
        public float3 SourcePoint;
        public Address DistanceFromSourcePointAddress;
        public Address DirFromSourcePointAddress;
        public Address NextCommandAddress;
    }
    
    [BurstCompile]
    public struct GetAimPointCommand : IScriptVizCommand
    {
        public InputEntityVar TargetEntity;
        public InputVar<float3> SourcePoint;
        public Address AimPointAddress;
        public Address DirFromSourcePointAddress;
        public Address DistanceFromSourcePointAddress;
        public Address NextCommandAddress;

        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (GetAimPointCommand*)commandData;
            
            Entity target;
            
            if (data->TargetEntity.Address.IsValid)
            {
                target = data->TargetEntity.Read(ref context);
            }
            else
            {
                target = context.OwnerEntity;
            }
            
            if (target == Entity.Null)
            {
                Debug.LogError($"Failed to get aim point - target entity is null, scriptviz entity: {context.OwnerEntity.Index}");
                return;
            }
            
            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            
            var request = new GetAimPointRequest();
            request.RequestEntity = context.OwnerEntity;
            request.TargetEntity = target;
            request.AimPointAddress = data->AimPointAddress;
            request.DirFromSourcePointAddress = data->DirFromSourcePointAddress;
            request.DistanceFromSourcePointAddress = data->DistanceFromSourcePointAddress;
            request.SourcePoint = data->SourcePoint.Read(ref context);
            request.NextCommandAddress = data->NextCommandAddress;
            context.Commands.AddComponent(context.SortIndex, requestEntity, request);
        }
    }
    
    [FriendlyName("Точка прицела (GET)")]
    [Serializable]
    public class GetAimPointNode : Node, ICommandNode, IPostWriteCommandNode
    {
        [HideInInspector]
        public EntitySocket TargetSocket = new();
        [HideInInspector]
        public Float3Socket AimPointOutSocket = new();
        [HideInInspector]
        public Float3Socket SourcePointSocket = new();
        [HideInInspector]
        public Float3Socket DirFromSourcePointOutSocket = new();
        [HideInInspector]
        public FloatSocket DistanceFromSourcePointOutSocket = new();

        [HideInInspector] public NodeInputSocket InputSocket = new();
        [HideInInspector] public NodeOutputSocket OutputSocket = new();
        
        public void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new GetAimPointCommand();
            compilerAllocator.InitializeInputVar(ref cmd.TargetEntity, TargetSocket);
            cmd.AimPointAddress = compilerAllocator.GetSocketAddress(AimPointOutSocket);
            cmd.DirFromSourcePointAddress = compilerAllocator.GetSocketAddress(DirFromSourcePointOutSocket);
            cmd.DistanceFromSourcePointAddress = compilerAllocator.GetSocketAddress(DistanceFromSourcePointOutSocket);
            compilerAllocator.InitializeInputVar(ref cmd.SourcePoint, SourcePointSocket);
            
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            InputSocket.In(sockets, "In");
            OutputSocket.Out(sockets, "Out");
            
            sockets.Add(new SocketInfo(TargetSocket, SocketType.In, "Источник"));
            sockets.Add(new SocketInfo(SourcePointSocket, SocketType.In, "Исходная точка"));
            
            sockets.Add(new SocketInfo(AimPointOutSocket, SocketType.Out, "Точка прицела (*)"));
            sockets.Add(new SocketInfo(DirFromSourcePointOutSocket, SocketType.Out, "Направление от исходной"));
            sockets.Add(new SocketInfo(DistanceFromSourcePointOutSocket, SocketType.Out, "Дистанция от исходной"));
        }

        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Точка прицела (* DELAYED!)";
        }

        public void OnPostCommandWrite(CompilerAllocator compilerAllocator, Address currentCommandAddress)
        {
            ref var cmd = ref compilerAllocator.GetCommandData<GetAimPointCommand>(currentCommandAddress);
            cmd.NextCommandAddress = compilerAllocator.GetFirstConnectedNodeAddress(OutputSocket);
        }
    }
}
