using System;
using System.Collections.Generic;
using Arena;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace TzarGames.GameCore.Abilities
{
    [Serializable]
    public struct ArenaAbilityAction : 
        IBufferElementData,
        IAbilityAction, 
        IAbilityActionJob<ArenaAbilityActionJob>
    {
        [SerializeField] private int calledId;
        [SerializeField] private byte actionId;

        public int CallerId
        {
            get => calledId;
            set => calledId = value;
        }
        public byte ActionId
        {
            get => actionId;
            set => actionId = value;
        }
    }
    
    [Serializable]
    public struct ArenaAbilityActionEventData : IBufferElementData, IAbilityScriptVizEvent, ICommandAddressData
    {
        [SerializeField] private Address abilityOwner;
        [SerializeField] private Address eventIdAddress;
        [SerializeField] private Address writeDataAddress;
        [SerializeField] private Address commandAddress;

        public Address StunDuration;
        
        public Address AbilityOwner
        {
            get => abilityOwner;
            set => abilityOwner = value;
        }

        public Address WriteDataAddress
        {
            get => writeDataAddress;
            set => writeDataAddress = value;
        }

        
        public Address CommandAddress
        {
            get => commandAddress;
            set => commandAddress = value;
        }
        
        public Address EventIdAddress
        {
            get => eventIdAddress;
            set => eventIdAddress = value;
        }
    }
    
    [Serializable]
    [FriendlyName("Ability action")]
    public class AbilityActionEventNode : BaseAbilityEventNode<ArenaAbilityActionEventData>
    {
        public AbilityDataSocket DataSocket = new();
        public IntSocket EventIdSocket = new();
        public FloatSocket StunDuration = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(EventIdSocket, SocketType.Out, "Event index"));
            sockets.Add(new SocketInfo(DataSocket, SocketType.Out, "Ability data"));
            sockets.Add(new SocketInfo(StunDuration, SocketType.Out, "Stun duration"));
        }

        protected override ArenaAbilityActionEventData GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.EventIdAddress = compiler.GetSocketAddress(EventIdSocket);
            result.WriteDataAddress = compiler.GetSocketAddress(DataSocket);
            result.StunDuration = compiler.GetSocketAddress(StunDuration);
            return result;
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "On ability action event";
        }
    }

    public class ArenaAbilityActionComponent : AbilityActionAsset<ArenaAbilityAction>
    {
        protected override ArenaAbilityAction Convert(IGCBaker baker)
        {
            return new ArenaAbilityAction
            {
            };
        }
    }

    public unsafe struct ArenaAbilityActionJob
    {
        [ReadOnly] public ComponentTypeHandle<Duration> DurationType;
        
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public BufferTypeHandle<EntityInstance> EntityInstanceArrayType;

        [ReadOnly] public BufferTypeHandle<ArenaAbilityActionEventData> AbilityActionEventDataType;
        [ReadOnly] public ComponentTypeHandle<StunDuration> StunDurationType;
        
        public void Execute(ArenaAbilityAction eventData, Entity abilityEntity,
            in AbilityOwner abilityOwner,
            int commandBufferIndex,
            EntityCommandBuffer.ParallelWriter commands,
            in AbilityInterface abilityInterface,
            ref DynamicBuffer<VariableDataByte> variableData,
            ref DynamicBuffer<EntityVariableData> entityVariableData,
            in DynamicBuffer<ConstantEntityVariableData> constantEntityVariableData,
            ref ScriptVizState state,
            in ScriptVizCodeInfo codeInfo,
            float deltaTime)
        {
            if (abilityInterface.HasComponent(AbilityActionEventDataType) == false)
            {
                return;
            }
            
            Duration duration = default;
            if (abilityInterface.HasComponent(DurationType))
            {
                duration = abilityInterface.GetComponent(DurationType);
            }

            EntityInstance* createdEntitiesPtr = default;
            int createdEntitiesCount = 0;
            if (abilityInterface.HasComponent(EntityInstanceArrayType))
            {
                var createdEntities = abilityInterface.GetBuffer(EntityInstanceArrayType);
                createdEntitiesPtr = (EntityInstance*)createdEntities.GetUnsafeReadOnlyPtr();
                createdEntitiesCount = createdEntities.Length;
            }
            
            var contextData = new ScriptVizAspect.ReadOnlyData(abilityEntity, variableData, entityVariableData, constantEntityVariableData, codeInfo);
            var owner = abilityOwner.Value;
            var events = abilityInterface.GetBuffer(AbilityActionEventDataType);

            StunDuration stunDuration = default;
            
            if (abilityInterface.HasComponent(StunDurationType))
            {
                stunDuration = abilityInterface.GetComponent(StunDurationType);
            }

            using (var contextHandle = new ContextDisposeHandle(ref state, ref contextData, ref commands, commandBufferIndex, deltaTime))
            {
                foreach (var evt in events)
                {
                    if (evt.EventIdAddress.IsValid)
                    {
                        contextHandle.Context.WriteToTemp(ref owner, evt.EventIdAddress);            
                    }

                    if (evt.StunDuration.IsValid)
                    {
                        contextHandle.Context.WriteToTemp(ref stunDuration.Value, evt.StunDuration);
                    }
                    if (evt.EventIdAddress.IsValid)
                    {
                        contextHandle.Context.WriteToTemp(eventData.ActionId, evt.EventIdAddress);
                    }
                    if (evt.WriteDataAddress.IsValid)
                    {
                        var durationPtr = (Duration*)UnsafeUtility.AddressOf(ref duration);
                        var writeData = new AbilityScriptVizWriteHandle(null, durationPtr, createdEntitiesPtr, createdEntitiesCount);
                        contextHandle.Context.WriteToTemp(ref writeData, evt.WriteDataAddress);
                    }
                    contextHandle.Execute(evt.CommandAddress);    
                }
            }
        }
    }
}