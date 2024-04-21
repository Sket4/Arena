using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.ScriptViz;
using TzarGames.GameCore.ScriptViz.Graph;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena.Abilities
{
    [Serializable]
    public struct ActionSequenceSharedData : IComponentData, IAbilityComponentJob<ActionSequenceAbilityJob>
    {
        [HideInInspector]
        public int CalledId;

        [HideInInspector]
        public int ScriptVizEventCallerID;

        [HideInInspector] public byte ScriptVizEventCount;
        
        public float NextActionAcceptTimeWindow;
        
        [NonSerialized]
        public int CurrentActionIndex;

        [NonSerialized] public AbilityID LastAcceptedPendingAbilityID;
        
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
                    var action = actions[i];

                    if (action.Actions.Length > ActionSequenceSharedData.MaxEventsPerAction)
                    {
                        throw new IndexOutOfRangeException($"Больше {ActionSequenceSharedData.MaxEventsPerAction} ивентов за один шаг не поддерживается");
                    }

                    for (var index = 0; index < action.Actions.Length; index++)
                    {
                        var eventAction = action.Actions[index];
                        eventAction.Bake(serializedData.CalledId, (byte)(index + i * ActionSequenceSharedData.MaxEventsPerAction), baker);
                    }

                    var bakedAction = new AbilitySequenceAction
                    {
                        Duration = action.Duration,
                        EventActivationTime = action.EventActivationTime,
                        EventCount = (byte)action.Actions.Length
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
            var callerId = TypeManager.GetTypeIndex<ActionSequenceAbilityEventNodeData>();
            
            Entities
                .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .ForEach((DynamicBuffer<ActionSequenceAbilityEventNodeData> nodeDatas, ref ActionSequenceSharedData sharedData) =>
                {
                    sharedData.ScriptVizEventCallerID = callerId;
                    sharedData.ScriptVizEventCount = (byte)nodeDatas.Length;
                    
                    for (var index = 0; index < nodeDatas.Length; index++)
                    {
                        var nodeData = nodeDatas[index];

                        nodeData.ActionId = (byte)index;
                        nodeData.CallerId = callerId;
                        
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

            for (int i = 0; i < sharedData.ScriptVizEventCount; i++)
            {
                actionCaller.CallAction(sharedData.ScriptVizEventCallerID, (byte)i);
            }
        }
    }
    [Serializable]
    [FriendlyName("On action sequence started")]
    public class OnActionSequenceStepEventNode : BaseAbilityEventNode<ActionSequenceAbilityEventNodeData>, ICustomNodeName
    {
        public IntSocket EventIdSocket = new();
        public FloatSocket DurationSocket = new();

        public override void DeclareSockets(List<SocketInfo> sockets)
        {
            base.DeclareSockets(sockets);
            sockets.Add(new SocketInfo(EventIdSocket, SocketType.Out, "Step index"));
            sockets.Add(new SocketInfo(DurationSocket, SocketType.Out, "Step duration"));
        }

        protected override ActionSequenceAbilityEventNodeData GetConvertedData(Entity entity, IGCBaker baker, ICompilerDataProvider compiler)
        {
            var result = base.GetConvertedData(entity, baker, compiler);
            result.SequenceActionIndexAddress = compiler.GetSocketAddress(EventIdSocket);
            result.DurationAddress = compiler.GetSocketAddress(DurationSocket);
            return result;
        }

        public string GetNodeName()
        {
            return "On action sequence started";
        }
    }
    
    
    [Serializable]
    public struct ActionSequenceAbilityEventNodeData : IBufferElementData, ICommandAddressData, IAbilityAction, IAbilityActionJob<ScriptVizSequenceActionJob>, IAbilityScriptVizEvent
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
    
    [BurstCompile]
    public struct ScriptVizSequenceActionJob
    {
        [ReadOnly]
        public BufferLookup<CodeDataByte> CodeDataLookup;
        [ReadOnly]
        public BufferLookup<ConstantEntityVariableData> ConstantEntityDataLookup;
        
        public void Execute(
            Entity abilityEntity,
            int commandBufferIndex,
            in AbilityOwner abilityOwner,
            UniversalCommandBuffer commands,
            ref DynamicBuffer<VariableDataByte> variableData,
            ref DynamicBuffer<EntityVariableData> entityVariableData,
            in ActionSequenceAbilityEventNodeData eventData,
            in ScriptVizCodeInfo codeInfo,
            in ActionSequenceSharedData sharedData,
            in Duration duration,
            float deltaTime)
        {
            var codeDataBytes = CodeDataLookup[codeInfo.CodeDataEntity];
            var constantEntityVariableDatas = ConstantEntityDataLookup[codeInfo.CodeDataEntity];
            var contextData = new ScriptVizAspect.ReadOnlyData(abilityEntity, variableData, entityVariableData, codeInfo);
            var owner = abilityOwner.Value;

            using (var contextHandle = new ContextDisposeHandle(codeDataBytes, constantEntityVariableDatas, ref contextData, ref commands, commandBufferIndex, deltaTime))
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

