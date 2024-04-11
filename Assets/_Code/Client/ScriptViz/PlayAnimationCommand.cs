using System;
using System.Collections;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
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
}
