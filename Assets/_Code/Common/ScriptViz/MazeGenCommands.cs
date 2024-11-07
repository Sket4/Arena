using System;
using System.Collections.Generic;
using Arena.Maze;
using TzarGames.GameCore;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.ScriptViz
{
    [Serializable]
    public struct OnZoneGateChangedEventCommand : IBufferElementData, ICommandAddressData
    {
        [SerializeField]
        Address commandAddress;

        public Address Zone1OutputAddress;
        public Address Zone2OutputAddress;

        public Address CommandAddress
        {
            get => commandAddress;
            set => commandAddress = value;
        }
    }
    
    [FriendlyName("On zone gate ID changed")]
    [Serializable]
    public class OnZoneGateChangedEventNode : DynamicBufferEventNode<OnZoneGateChangedEventCommand>
    {
        public IntSocket Zone1_OutSocket = new();
        public IntSocket Zone2_OutSocket = new();
        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "On zone gate ID changed";
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(Zone1_OutSocket, SocketType.Out, "Zone 1 ID"));
            sockets.Add(new SocketInfo(Zone2_OutSocket, SocketType.Out, "Zone 2 ID"));
        }

        protected override OnZoneGateChangedEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.Zone1OutputAddress = compiler.GetSocketAddress(Zone1_OutSocket);
            result.Zone2OutputAddress = compiler.GetSocketAddress(Zone2_OutSocket);
            return result;
        }
    }
    
    [Serializable]
    public struct OnZoneChangedEventCommand : IBufferElementData, ICommandAddressData
    {
        [SerializeField]
        Address commandAddress;

        public Address ZoneIDOutputAddress;

        public Address CommandAddress
        {
            get => commandAddress;
            set => commandAddress = value;
        }
    }
    
    [FriendlyName("On zone ID changed")]
    [Serializable]
    public class OnZoneChangedEventNode : DynamicBufferEventNode<OnZoneChangedEventCommand>
    {
        public IntSocket ZoneID_OutSocket = new();
        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "On zone ID changed";
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(ZoneID_OutSocket, SocketType.Out, "Zone ID"));
        }

        protected override OnZoneChangedEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.ZoneIDOutputAddress = compiler.GetSocketAddress(ZoneID_OutSocket);
            return result;
        }
    }
    
    [Serializable]
    public struct OnZoneActivatedEventCommand : IBufferElementData, ICommandAddressData
    {
        [SerializeField]
        Address commandAddress;

        public Address ZoneIDOutputAddress;

        public Address CommandAddress
        {
            get => commandAddress;
            set => commandAddress = value;
        }
    }
    
    [FriendlyName("On zone activated")]
    [Serializable]
    public class OnZoneActivatedEventNode : DynamicBufferEventNode<OnZoneActivatedEventCommand>
    {
        public IntSocket ZoneID_OutSocket = new();
        public override bool ShowEditableProperties => false;

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "On zone activated";
        }

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(ZoneID_OutSocket, SocketType.Out, "Zone ID"));
        }

        protected override OnZoneActivatedEventCommand GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.ZoneIDOutputAddress = compiler.GetSocketAddress(ZoneID_OutSocket);
            return result;
        }
    }
    
    [BurstCompile]
    public struct ActivateZoneRequestCommand : IScriptVizCommand
    {
        public InputVar<int> ZoneIdVariable;
        
        [BurstCompile]
        [AOT.MonoPInvokeCallback(typeof(ScriptVizCommandRegistry.ExecuteDelegate))]
        public static unsafe void Execute(ref Context context, void* commandData)
        {
            var data = (ActivateZoneRequestCommand*)commandData;

            var zoneId = data->ZoneIdVariable.Read(ref context);

            var requestData = new ActivateZoneRequest
            {
                Zone = new ZoneId((ushort)zoneId)
            };

            var requestEntity = context.Commands.CreateEntity(context.SortIndex);
            context.Commands.AddComponent(context.SortIndex, requestEntity, requestData);
        }
    }
    
    [Serializable]
    [FriendlyName("Активировать зону лабиринта")]
    public class ActivateZoneRequestCommandNode : CommandNode
    {
        [HideInInspector] public IntSocket ZoneIdSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(ZoneIdSocket, SocketType.In, "ИД зоны"));
        }

        public override void WriteCommand(CompilerAllocator compilerAllocator, out Address commandAddress)
        {
            var cmd = new ActivateZoneRequestCommand();
            
            compilerAllocator.InitializeInputVar(ref cmd.ZoneIdVariable, ZoneIdSocket);

            commandAddress = compilerAllocator.WriteCommand(ref cmd);
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Активировать зону лабиринта";
        }
    }
}
