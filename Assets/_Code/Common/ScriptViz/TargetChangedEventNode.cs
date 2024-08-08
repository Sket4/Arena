using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Commands;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Arena.ScriptViz
{
    public struct TargetChangedEventPreviousTarget : IComponentData
    {
        public Entity Value;
    }
    
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct GameplayCommandsBakingSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity)
                     in SystemAPI.Query<DynamicBuffer<OnTargetChangedEventCommand>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                ecb.AddComponent<TargetChangedEventPreviousTarget>(entity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
    
    [FriendlyName("On target changed")]
    [Serializable]
    public class OnTargetChangedEventNode : DynamicBufferEventNode<OnTargetChangedEventCommand>
    {
        public EntitySocket TargetEntityOutSocket = new();
        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraph.ScriptVizGraphPage page)
        {
            return "On target changed";
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(TargetEntityOutSocket, SocketType.Out, "Target"));
        }

        protected override OnTargetChangedEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.TargetEntityOutputAddress = compiler.GetSocketAddress(TargetEntityOutSocket);
            return result;
        }
    }    
}
