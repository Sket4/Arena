using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Entities;

namespace Arena.ScriptViz
{
    public struct InteractionEventCommand : IBufferElementData, ICommandAddressData
    {
        public Address InteractorEntityOutputAddress;
        public Address CommandAddress { get; set; }
    }
    
    [FriendlyName("On interact")]
    [Serializable]
    public class InteractionEventNode : DynamicBufferEventNode<InteractionEventCommand>
    {
        public EntitySocket InteractorEntityOutSocket = new();
        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "On interact";
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(InteractorEntityOutSocket, SocketType.Out, "Interactor"));
        }

        protected override InteractionEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.InteractorEntityOutputAddress = compiler.GetSocketAddress(InteractorEntityOutSocket);
            return result;
        }
    }    
}
