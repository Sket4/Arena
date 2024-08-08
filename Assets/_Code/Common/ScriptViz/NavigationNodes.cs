using System;
using System.Collections;
using System.Collections.Generic;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Arena.ScriptViz
{
    struct MoveOnPathCommand : IScriptVizCommand
    {
        public InputEntityVar MovementEntity;
        public InputEntityVar PathEntity;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (MoveOnPathCommand*)commandData;

            var pathEntity = data->PathEntity.Read(ref context);

            if (pathEntity == Entity.Null)
            {
                Debug.LogError($"path is empty, caller: {context.OwnerEntity.Index}");
                return;
            }

            var mover = data->MovementEntity.Read(ref context);

            if (mover == Entity.Null)
            {
                mover = context.OwnerEntity;
            }

            var entityRequest = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.SetComponent(context.SortIndex, mover, new SplinePathMovement
            {
                TargetPathEntity = pathEntity
            });
            context.Commands.SetComponentEnabled<SplinePathMovement>(context.SortIndex, mover, true);
        }
    }
    
    [Serializable]
    [FriendlyName("Двигаться по пути")]
    public class MoveOnPathNode : CommandNode
    {
        public EntitySocket MovingEntitySocket;
        public EntitySocket PathEntitySocket;
        
        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new MoveOnPathCommand();
            compilerAllocator.InitializeInputVar(ref cmd.MovementEntity, MovingEntitySocket);
            compilerAllocator.InitializeInputVar(ref cmd.PathEntity, PathEntitySocket);
            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            return "Двигаться по пути";
        }

        public override bool ShowEditableProperties => false;
        
        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            
            sockets.Add(new SocketInfo(PathEntitySocket, SocketType.In, "Путь"));
            sockets.Add(new SocketInfo(MovingEntitySocket, SocketType.In, "Двигатель (опц)"));
        }
    }
}