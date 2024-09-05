using System;
using System.Collections.Generic;
using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.ScriptViz
{
    [BurstCompile]
    public struct PlayAnimationCommand : IScriptVizCommand
    {
        public InputVar<int> AnimationID;
        public InputEntityVar TargetAnimator;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (PlayAnimationCommand*)commandData;
            
            var animEvent = context.Commands.CreateEntity(context.SortIndex);
            int animId;
            
            animId = data->AnimationID.ReadConstant(ref context);

            var targetAnimator = data->TargetAnimator.Read(ref context);
            
            context.Commands.AddComponent(context.SortIndex, animEvent, new AnimationPlayEvent
            {
                AnimationID = animId,
                AutoDestroy = true
            });
            context.Commands.AddComponent(context.SortIndex, animEvent, new Target { Value = targetAnimator });
            
            Debug.Log($"Play anim command {animId} on entity {targetAnimator.Index}:{targetAnimator.Version}");
        }
    }

    [Serializable]
    [FriendlyName("Play animation")]
    public class PlayAnimationCommandNode : CommandNode
    {
        [HideInInspector]
        public EntitySocket TargetAnimatorSocket = new EntitySocket();
        public AnimationID AnimationID;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            if (AnimationID == null)
            {
                Debug.LogError($"Null animation id for node with id {ID}");
                commandAddress = Address.Invalid;
                return;
            }
            
            var cmd = new PlayAnimationCommand();
            compilerAllocator.InitializeInputVar(ref cmd.TargetAnimator, TargetAnimatorSocket);
            cmd.AnimationID.Address = compilerAllocator.WriteConstantDataAndGetAddress(AnimationID.Id);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetAnimatorSocket, SocketType.In, "Target animator"));
        }
    }
    
    [BurstCompile]
    public struct PlayCharacterAnimationCommand : IScriptVizCommand
    {
        public InputVar<int> AnimationID;
        public InputEntityVar TargetAnimator;
        public InputVar<float> TransitionTime;
        public InputVar<float> Duration;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (PlayCharacterAnimationCommand*)commandData;
            
            var animEvent = context.Commands.CreateEntity(context.SortIndex);
            var animId = data->AnimationID.ReadConstant(ref context);
            var targetAnimator = data->TargetAnimator.Read(ref context);
            var transitionTime = data->TransitionTime.Read(ref context);
            
            context.Commands.AddComponent(context.SortIndex, animEvent, new CharacterAnimationPlayEvent 
            { 
                Target = targetAnimator,
                AnimationID = animId,
                UseTransitionTime = true,
                TransitionTime = transitionTime
            });
            if(data->Duration.Address.IsValid)
            {
                var duration = data->Duration.Read(ref context);
                
                context.Commands.AddComponent(context.SortIndex, animEvent, new Duration
                {
                    BaseValue = duration,
                    Value = duration
                });
                //Debug.Log($"Play character animation {animId} with transition time {transitionTime} and duration {duration}");
            }
            //else
            //{
                //Debug.Log($"Play character animation {animId} with transition time {transitionTime}");
            //}
        }
    }
    
    [Serializable]
    [FriendlyName("Запустить анимацию персонажа")]
    public class PlayCharacterAnimationCommandNode : CommandNode
    {
        [HideInInspector]
        public EntitySocket TargetAnimatorSocket = new();
        
        public AnimationID AnimationID;
        
        [HideInInspector]
        public FloatSocket TransitionTimeSocket = new(0.2f);
        
        [HideInInspector]
        public FloatSocket DurationSocket = new();
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            if (AnimationID == null)
            {
                Debug.LogError($"Null animation id for node with id {ID}");
                commandAddress = Address.Invalid;
                return;
            }
            
            var cmd = new PlayCharacterAnimationCommand();
            compilerAllocator.InitializeInputVar(ref cmd.TargetAnimator, TargetAnimatorSocket);
            cmd.AnimationID.Address = compilerAllocator.WriteConstantDataAndGetAddress(AnimationID.Id);
            compilerAllocator.InitializeInputVar(ref cmd.TransitionTime, TransitionTimeSocket);
            compilerAllocator.InitializeInputVar(ref cmd.Duration, DurationSocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetAnimatorSocket, SocketType.In, "Персонаж"));
            sockets.Add(new SocketInfo(TransitionTimeSocket, SocketType.In, "Время перехода"));
            sockets.Add(new SocketInfo(DurationSocket, SocketType.In, "Длительность", SocketInfoFlags.HideEditor));
        }
        
        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (AnimationID)
            {
                return $"Запустить анимацию {AnimationID.name}";
            }
            return "Запустить анимацию";
        }
    }
    
    [BurstCompile]
    public struct StopCharacterAnimationCommand : IScriptVizCommand
    {
        public InputVar<int> AnimationID;
        public InputEntityVar TargetAnimator;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Exec(ref Context context, void* commandData)
        {
            var data = (StopCharacterAnimationCommand*)commandData;
            
            var animEvent = context.Commands.CreateEntity(context.SortIndex);
            var animId = data->AnimationID.ReadConstant(ref context);
            var targetAnimator = data->TargetAnimator.Read(ref context);
            
            context.Commands.AddComponent(context.SortIndex, animEvent, new CharacterAnimationStopEvent 
            { 
                Target = targetAnimator,
                AnimationID = animId,
            });
        }
    }
    
    [Serializable]
    [FriendlyName("Остановить анимацию персонажа")]
    public class StopCharacterAnimationCommandNode : CommandNode
    {
        [HideInInspector]
        public EntitySocket TargetAnimatorSocket = new();
        
        public AnimationID AnimationID;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            if (AnimationID == null)
            {
                Debug.LogError($"Null animation id for node with id {ID}");
                commandAddress = Address.Invalid;
                return;
            }
            
            var cmd = new StopCharacterAnimationCommand();
            compilerAllocator.InitializeInputVar(ref cmd.TargetAnimator, TargetAnimatorSocket);
            cmd.AnimationID.Address = compilerAllocator.WriteConstantDataAndGetAddress(AnimationID.Id);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetAnimatorSocket, SocketType.In, "Персонаж"));
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (AnimationID)
            {
                return $"Стоп анимации {AnimationID.name} персонажа";
            }
            return "Стоп анимации персонажа";
        }
    }

    public struct AnimationEventCommand : IBufferElementData, ICommandAddressData
    {
        public int EventID;
        public Address CommandAddress;
        Address ICommandAddressData.CommandAddress { get => CommandAddress; set => CommandAddress = value; }
    }

    [Serializable]
    [FriendlyName("Событие анимации")]
    public class AnimationEventNode : DynamicBufferEventNode<AnimationEventCommand>
    {
        public StringID ID;

        protected override AnimationEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.EventID = ID ? ID.Id : -1;
            return result;
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            if (ID)
            {
                return $"Событие анимации {ID.name}";
            }
            return "Событие анимации";
        }
    }
}
