using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Abilities
{
    [Serializable]
    public struct ActionSequenceSharedData : IComponentData, IAbilityComponentJob<ActionSequenceAbilityJob>
    {
        [HideInInspector]
        public int CalledId;

        [HideInInspector]
        public int ScriptVizStartEventCallerID;

        [HideInInspector] public byte ScriptVizStartEventCount;
        
        [HideInInspector]
        public int ScriptVizTimerEventCallerID;

        [HideInInspector] public byte ScriptVizTimerEventCount;
        
        public float NextActionAcceptTimeWindow;
        
        [NonSerialized]
        public int CurrentActionIndex;

        [NonSerialized] public AbilityID LastAcceptedPendingAbilityID;
        public int CurrentTimerEventIndex;

        public const int MaxEventsPerAction = 10;
    }

    [Serializable]
    public struct AbilitySequenceAction : IBufferElementData
    {
        public float Duration;
        public float EventActivationTime;
        public bool IsEventsActivated;
        public byte EventCount;
    }

    [DisallowMultipleComponent]
    [UseDefaultInspector()]
    public class ActionSequenceAbilityComponent : ComponentDataBehaviour<ActionSequenceSharedData>
    {
        [Serializable]
        class AbilitySequenceActionAuthoring
        {
            public float Duration = 1;
            [Range(0,1)]
            public float EventActivationTime = 0.5f;
            public AbilityActionAssetBase[] Actions;
        }
        
        [SerializeField]
        ActionSequenceSharedData sharedData;
            
        [SerializeField] private AbilitySequenceActionAuthoring[] actions;

        protected override ActionSequenceSharedData CreateDefaultValue()
        {
            return new ActionSequenceSharedData
            {
                NextActionAcceptTimeWindow = 0.25f
            };
        }

        protected override void Bake<K>(ref ActionSequenceSharedData serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            serializedData.CalledId = TypeManager.GetTypeIndex<ActionSequenceSharedData>();

            var bakedActions = baker.AddBuffer<AbilitySequenceAction>();
            
            if (actions != null)
            {
                for (var i = 0; i < actions.Length; i++)
                {
                    var sequenceAction = actions[i];

                    if (sequenceAction.Actions.Length > ActionSequenceSharedData.MaxEventsPerAction)
                    {
                        throw new IndexOutOfRangeException($"Больше {ActionSequenceSharedData.MaxEventsPerAction} ивентов за один шаг не поддерживается");
                    }

                    for (var index = 0; index < sequenceAction.Actions.Length; index++)
                    {
                        var eventAction = sequenceAction.Actions[index];
                        eventAction.Bake(serializedData.CalledId, (byte)(index + i * ActionSequenceSharedData.MaxEventsPerAction), baker);
                    }

                    var bakedAction = new AbilitySequenceAction
                    {
                        Duration = sequenceAction.Duration,
                        EventActivationTime = sequenceAction.EventActivationTime,
                        EventCount = (byte)sequenceAction.Actions.Length
                    };
                    bakedActions.Add(bakedAction);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class ActionSequenceBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var startCallerId = TypeManager.GetTypeIndex<ActionSequenceStartedAbilityEventNodeData>();
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((DynamicBuffer<ActionSequenceStartedAbilityEventNodeData> nodeDatas, ref ActionSequenceSharedData sharedData) =>
                {
                    sharedData.ScriptVizStartEventCallerID = startCallerId;
                    sharedData.ScriptVizStartEventCount = (byte)nodeDatas.Length;
                    
                    for (var index = 0; index < nodeDatas.Length; index++)
                    {
                        var nodeData = nodeDatas[index];

                        nodeData.ActionId = (byte)index;
                        nodeData.CallerId = startCallerId;
                        
                        nodeDatas[index] = nodeData;
                    }
                }).Run();
            
            var timerCallerId = TypeManager.GetTypeIndex<ActionSequenceTimerEventNodeData>();
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((DynamicBuffer<ActionSequenceTimerEventNodeData> nodeDatas, ref ActionSequenceSharedData sharedData) =>
                {
                    sharedData.ScriptVizTimerEventCallerID = timerCallerId;
                    sharedData.ScriptVizTimerEventCount = (byte)nodeDatas.Length;
                    
                    for (var index = 0; index < nodeDatas.Length; index++)
                    {
                        var nodeData = nodeDatas[index];

                        nodeData.ActionId = (byte)index;
                        nodeData.CallerId = timerCallerId;
                        
                        nodeDatas[index] = nodeData;
                    }
                }).Run();
        }
    }

    public struct ActionSequenceAbilityJob
    {
        [ReadOnly]
        public ComponentLookup<PlayerInput> PlayerInputFromEntity;

        [MethodPriority(AbilitySystem.DefaultHighPriority)]
        public void OnStarted(ref Duration duration, ref ActionSequenceSharedData sharedData, ref DynamicBuffer<AbilitySequenceAction> actions, ref ActionCaller caller)
        {
            sharedData.LastAcceptedPendingAbilityID = AbilityID.Null;
            sharedData.CurrentActionIndex = 0;

            for (var index = 0; index < actions.Length; index++)
            {
                var sequenceAction = actions[index];
                sequenceAction.IsEventsActivated = false;
                actions[index] = sequenceAction;
            }
            
            StartAction(actions[0], in sharedData, ref caller, ref duration);
        }
        
        [MethodPriority(AbilitySystem.DefaultLowPriority)]
        public void OnUpdate(in AbilityOwner abilityOwner, in AbilityID abilityId, ref ActionCaller actionCaller, ref ActionSequenceSharedData sharedData, ref AbilityControl abilityControl, ref Duration duration, ref DynamicBuffer<AbilitySequenceAction> actions)
        {
            var input = PlayerInputFromEntity[abilityOwner.Value];

            var currentAction = actions[sharedData.CurrentActionIndex];

            if (currentAction.Duration - duration.ElapsedTime < sharedData.NextActionAcceptTimeWindow
                && input.PendingAbilityID != AbilityID.Null)
            {
                // игрок попытался применить некоторое умение (это же самое или другое) ближе к концу текущего шага
                sharedData.LastAcceptedPendingAbilityID = input.PendingAbilityID;
            }

            if (currentAction.IsEventsActivated == false && duration.ElapsedTime / currentAction.Duration >= currentAction.EventActivationTime)
            {
                currentAction.IsEventsActivated = true;
                actions[sharedData.CurrentActionIndex] = currentAction;
                
                for (byte i = 0; i < currentAction.EventCount; i++)
                {
                    actionCaller.CallAction(sharedData.CalledId, (byte)(sharedData.CurrentActionIndex * ActionSequenceSharedData.MaxEventsPerAction + i));
                }
                
                for (int i = 0; i < sharedData.ScriptVizTimerEventCount; i++)
                {
                    sharedData.CurrentTimerEventIndex = i;
                    actionCaller.CallAction(sharedData.ScriptVizTimerEventCallerID, (byte)i);
                }
            }

            if (duration.ElapsedTime < currentAction.Duration)
            {
                // время текущего действия еще не истекло, ничего не делаем
                return;
            }

            if (sharedData.LastAcceptedPendingAbilityID != abilityId)
            {
                // следующее умение отличается от текущего, поэтому прерываем умение
                abilityControl.StopRequest = true;
                return;
            }
            
            sharedData.CurrentActionIndex++;
            
            if (sharedData.CurrentActionIndex >= actions.Length)
            {
                // последовательность действий окончена, прерываем умение
                abilityControl.StopRequest = true;
                return;
            }

            duration.ElapsedTime -= currentAction.Duration;
            
            currentAction = actions[sharedData.CurrentActionIndex];
            abilityControl.StopRequest = false;
            sharedData.LastAcceptedPendingAbilityID = AbilityID.Null;
            StartAction(in currentAction, in sharedData, ref actionCaller, ref duration);
        }

        private static void StartAction(in AbilitySequenceAction currentAction, in ActionSequenceSharedData sharedData, ref ActionCaller actionCaller, ref Duration duration)
        {
            duration.Value = currentAction.Duration;

            for (int i = 0; i < sharedData.ScriptVizStartEventCount; i++)
            {
                actionCaller.CallAction(sharedData.ScriptVizStartEventCallerID, (byte)i);
            }
        }
    }
    [Serializable]
    [FriendlyName("Action sequence started")]
    public class OnActionSequenceStepEventNode : BaseAbilityEventNode<ActionSequenceStartedAbilityEventNodeData>
    {
        public IntSocket EventIdSocket = new();
        public FloatSocket DurationSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(EventIdSocket, SocketType.Out, "Step index"));
            sockets.Add(new SocketInfo(DurationSocket, SocketType.Out, "Step duration"));
        }

        protected override ActionSequenceStartedAbilityEventNodeData GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.SequenceActionIndexAddress = compiler.GetSocketAddress(EventIdSocket);
            result.DurationAddress = compiler.GetSocketAddress(DurationSocket);
            return result;
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Action sequence started";
        }
    }
    [Serializable]
    [FriendlyName("Action sequence timer event")]
    public class OnActionSequenceTimerEventNode : BaseAbilityEventNode<ActionSequenceTimerEventNodeData>
    {
        public IntSocket EventIdSocket = new();
        public IntSocket TimerEventSocket = new();
        public QuaternionSocket RotationSocket = new();
        public EntitySocket LastInstanceSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(EventIdSocket, SocketType.Out, "Step index"));
            sockets.Add(new SocketInfo(TimerEventSocket, SocketType.Out, "Timer event index"));
            sockets.Add(new SocketInfo(RotationSocket, SocketType.Out, "Rotation"));
            sockets.Add(new SocketInfo(LastInstanceSocket, SocketType.Out, "Last entity instance"));
        }

        protected override ActionSequenceTimerEventNodeData GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.SequenceActionIndexAddress = compiler.GetSocketAddress(EventIdSocket);
            result.TimerEventIndexAddress = compiler.GetSocketAddress(TimerEventSocket);
            result.RotationAddress = compiler.GetSocketAddress(RotationSocket);
            result.LatestInstanceAddress = compiler.GetSocketAddress(LastInstanceSocket);
            return result;
        }

        public override string GetNodeName(ScriptVizGraphPage page)
        {
            return "Action sequence timer event";
        }
    }
    
    
    [Serializable]
    public struct ActionSequenceStartedAbilityEventNodeData : IBufferElementData, ICommandAddressData, IAbilityAction, IAbilityActionJob<ScriptVizSequenceStartActionJob>, IAbilityScriptVizEvent
    {
        [SerializeField] private Address commandAddress;
        public Address CommandAddress { get => commandAddress; set => commandAddress = value; }
        
        [SerializeField] private Address AbilityOwnerAddress;
        
        public Address SequenceActionIndexAddress;
        public Address DurationAddress;
        
        public Address AbilityOwner
        {
            get => AbilityOwnerAddress;
            set => AbilityOwnerAddress = value;
        }

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
    public struct ActionSequenceTimerEventNodeData : IBufferElementData, ICommandAddressData, IAbilityAction, IAbilityActionJob<ScriptVizSequenceTimerEventActionJob>, IAbilityScriptVizEvent
    {
        [SerializeField] private Address commandAddress;
        public Address CommandAddress { get => commandAddress; set => commandAddress = value; }
        
        [SerializeField] private Address AbilityOwnerAddress;
        
        public Address SequenceActionIndexAddress;
        public Address TimerEventIndexAddress;
        public Address RotationAddress;
        public Address LatestInstanceAddress;
        
        public Address AbilityOwner
        {
            get => AbilityOwnerAddress;
            set => AbilityOwnerAddress = value;
        }

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
    
    [BurstCompile]
    public struct ScriptVizSequenceTimerEventActionJob
    {
        public void Execute(
            Entity abilityEntity,
            int commandBufferIndex,
            in AbilityOwner abilityOwner,
            EntityCommandBuffer.ParallelWriter commands,
            ref DynamicBuffer<VariableDataByte> variableData,
            ref DynamicBuffer<EntityVariableData> entityVariableData,
            ref DynamicBuffer<ConstantEntityVariableData> constantEntityVariableData,

            in DynamicBuffer<EntityInstance> entityInstances,
            ref ScriptVizState state,
            in ActionSequenceTimerEventNodeData eventData,
            in ScriptVizCodeInfo codeInfo,
            in ActionSequenceSharedData sharedData,
            in LocalTransform transform,
            float deltaTime)
        {
            var contextData = new ScriptVizAspect.ReadOnlyData(abilityEntity, variableData, entityVariableData, constantEntityVariableData, codeInfo);
            var owner = abilityOwner.Value;

            using (var contextHandle = new ContextDisposeHandle(ref state, ref contextData, ref commands, commandBufferIndex, deltaTime))
            {
                if (eventData.AbilityOwner.IsValid)
                {
                    contextHandle.Context.WriteToTemp(ref owner, eventData.AbilityOwner);
                }
                if (eventData.SequenceActionIndexAddress.IsValid)
                {
                    contextHandle.Context.WriteToTemp(sharedData.CurrentActionIndex, eventData.SequenceActionIndexAddress);
                }
                if (eventData.TimerEventIndexAddress.IsValid)
                {
                    //Debug.Log($"Writing duration {duration.Value} to event {sharedData.CurrentActionIndex}");
                    contextHandle.Context.WriteToTemp(sharedData.CurrentTimerEventIndex, eventData.TimerEventIndexAddress);
                }

                if (eventData.LatestInstanceAddress.IsValid)
                {
                    var writeData = entityInstances.Length > 0
                        ? entityInstances[entityInstances.Length-1].Value
                        : Entity.Null; 
                    
                    contextHandle.Context.WriteToTemp(ref writeData, eventData.LatestInstanceAddress);
                }

                if (eventData.RotationAddress.IsValid)
                {
                    contextHandle.Context.WriteToTemp(transform.Rotation, eventData.RotationAddress);
                }
                
                contextHandle.Execute(eventData.CommandAddress);
            }
        }
    }
    
    [BurstCompile]
    public struct ScriptVizSequenceStartActionJob
    {
        public void Execute(
            Entity abilityEntity,
            int commandBufferIndex,
            in AbilityOwner abilityOwner,
            EntityCommandBuffer.ParallelWriter commands,
            ref DynamicBuffer<VariableDataByte> variableData,
            ref DynamicBuffer<EntityVariableData> entityVariableData,
            ref DynamicBuffer<ConstantEntityVariableData> constantEntityVariableData,
            ref ScriptVizState state,
            in ActionSequenceStartedAbilityEventNodeData eventData,
            in ScriptVizCodeInfo codeInfo,
            in ActionSequenceSharedData sharedData,
            in Duration duration,
            float deltaTime)
        {
            var contextData = new ScriptVizAspect.ReadOnlyData(abilityEntity, variableData, entityVariableData, constantEntityVariableData, codeInfo);
            var owner = abilityOwner.Value;

            using (var contextHandle = new ContextDisposeHandle(ref state, ref contextData, ref commands, commandBufferIndex, deltaTime))
            {
                if (eventData.AbilityOwner.IsValid)
                {
                    contextHandle.Context.WriteToTemp(ref owner, eventData.AbilityOwner);
                }
                if (eventData.SequenceActionIndexAddress.IsValid)
                {
                    contextHandle.Context.WriteToTemp(sharedData.CurrentActionIndex, eventData.SequenceActionIndexAddress);
                }
                if (eventData.DurationAddress.IsValid)
                {
                    //Debug.Log($"Writing duration {duration.Value} to event {sharedData.CurrentActionIndex}");
                    contextHandle.Context.WriteToTemp(duration.Value, eventData.DurationAddress);
                }
                
                contextHandle.Execute(eventData.CommandAddress);
            }
        }
    }
}

