using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;

namespace TzarGames.GameCore.Abilities.Generated
{
	// GENERATED CODE, DO NOT MODIFY
	[BurstCompile]
	public struct Meleeattackability_19_AbilityJob : IJobChunk
	{
		public EntityCommandBuffer.ParallelWriter Commands;
		public EntityArchetype AbilityEventArchetype;
		public bool IsServer;
		public float GlobalDeltaTime;
		[ReadOnly] public ComponentLookup<DeltaTime> DeltaTimeFromEntity;
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.PlayerController> PlayerControllerLookup;
		[ReadOnly] public ComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer> NetworkPlayerLookup;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState> AbilityStateType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID> AbilityIDType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.Duration> DurationType;
		public EntityTypeHandle EntityType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner> AbilityOwnerType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.ScriptViz.VariableDataByte> VariableDataByteType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.ScriptViz.EntityVariableData> EntityVariableDataType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData> ConstantEntityVariableDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizState> ScriptVizStateType;
		[ReadOnly] public SharedComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo> ScriptVizCodeInfoType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate> CopyOwnerTransformToAbilityOnUpdateType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<Unity.Transforms.LocalTransform> LocalTransformType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation> AddOwnerAttackVerticalOffsetAsTranslationType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<Arena.Abilities.ActionSequenceSharedData> ActionSequenceSharedDataType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<Arena.Abilities.AbilitySequenceAction> AbilitySequenceActionType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection> ConvertRotationToDirectionType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Direction> DirectionType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Radius> RadiusType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.MoveToTargetAbilityComponentData> MoveToTargetAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility> CopyOwnerCriticalToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier> CriticalDamageMultiplierType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance> CriticalDamageChanceType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility> CopyOwnerDamageToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Damage> DamageType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility> CopyOwnerRadiusToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.MinimalRadius> MinimalRadiusType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart> CopyOwnerTransformToAbilityOnStartType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics> ModifyOwnerCharacteristicsType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<TzarGames.GameCore.Abilities.HitQueryAbilityAction> HitQueryAbilityActionType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<Arena.Abilities.ActionSequenceStartedAbilityEventNodeData> ActionSequenceStartedAbilityEventNodeDataType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.EntityInstance> EntityInstanceType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<Arena.Abilities.ActionSequenceTimerEventNodeData> ActionSequenceTimerEventNodeDataType;

		public TzarGames.GameCore.Abilities.MoveToTargetAbilityComponentJob _MoveToTargetAbilityComponentJob;
		public Arena.Abilities.ActionSequenceAbilityJob _ActionSequenceAbilityJob;
		public TzarGames.GameCore.Abilities.AttackHeightJob _AttackHeightJob;
		public TzarGames.GameCore.Abilities.ConvertRotationToDirectionJob _ConvertRotationToDirectionJob;
		public TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbilityJob _CopyOwnerCriticalToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerDamageToAbilityJob _CopyOwnerDamageToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbilityJob _CopyOwnerRadiusToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStartJob _CopyOwnerTransformToAbilityOnStartJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdateJob _CopyOwnerTransformToAbilityOnUpdateJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.ModifyOwnerCharacteristicsJob _ModifyOwnerCharacteristicsJob;
		public TzarGames.GameCore.Abilities.AbilityCylinderHitActionJob _AbilityCylinderHitActionJob;
		public Arena.Abilities.ScriptVizSequenceStartActionJob _ScriptVizSequenceStartActionJob;
		public Arena.Abilities.ScriptVizSequenceTimerEventActionJob _ScriptVizSequenceTimerEventActionJob;
		public TzarGames.GameCore.Abilities.ScriptVizAbilityJob _ScriptVizAbilityJob;


		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			var AbilityStateArray = chunk.GetNativeArray(ref AbilityStateType);
			var AbilityIDArray = chunk.GetNativeArray(ref AbilityIDType);
			var DurationArray = chunk.GetNativeArray(ref DurationType);
			var EntityArray = chunk.GetNativeArray(EntityType);
			var AbilityOwnerArray = chunk.GetNativeArray(ref AbilityOwnerType);
			var VariableDataByteAccessor = chunk.GetBufferAccessor(ref VariableDataByteType);
			var EntityVariableDataAccessor = chunk.GetBufferAccessor(ref EntityVariableDataType);
			var ConstantEntityVariableDataAccessor = chunk.GetBufferAccessor(ref ConstantEntityVariableDataType);
			var ScriptVizStateArray = chunk.GetNativeArray(ref ScriptVizStateType);
			var _ScriptVizCodeInfo = chunk.GetSharedComponent(ScriptVizCodeInfoType);
			var CopyOwnerTransformToAbilityOnUpdateArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnUpdateType);
			var LocalTransformArray = chunk.GetNativeArray(ref LocalTransformType);
			var AddOwnerAttackVerticalOffsetAsTranslationArray = chunk.GetNativeArray(ref AddOwnerAttackVerticalOffsetAsTranslationType);
			var ActionSequenceSharedDataArray = chunk.GetNativeArray(ref ActionSequenceSharedDataType);
			var AbilitySequenceActionAccessor = chunk.GetBufferAccessor(ref AbilitySequenceActionType);
			var ConvertRotationToDirectionArray = chunk.GetNativeArray(ref ConvertRotationToDirectionType);
			var DirectionArray = chunk.GetNativeArray(ref DirectionType);
			var RadiusArray = chunk.GetNativeArray(ref RadiusType);
			var MoveToTargetAbilityComponentDataArray = chunk.GetNativeArray(ref MoveToTargetAbilityComponentDataType);
			var CopyOwnerCriticalToAbilityArray = chunk.GetNativeArray(ref CopyOwnerCriticalToAbilityType);
			var CriticalDamageMultiplierArray = chunk.GetNativeArray(ref CriticalDamageMultiplierType);
			var CriticalDamageChanceArray = chunk.GetNativeArray(ref CriticalDamageChanceType);
			var CopyOwnerDamageToAbilityArray = chunk.GetNativeArray(ref CopyOwnerDamageToAbilityType);
			var DamageArray = chunk.GetNativeArray(ref DamageType);
			var CopyOwnerRadiusToAbilityArray = chunk.GetNativeArray(ref CopyOwnerRadiusToAbilityType);
			var MinimalRadiusArray = chunk.GetNativeArray(ref MinimalRadiusType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var ModifyOwnerCharacteristicsArray = chunk.GetNativeArray(ref ModifyOwnerCharacteristicsType);
			var HitQueryAbilityActionAccessor = chunk.GetBufferAccessor(ref HitQueryAbilityActionType);
			var ActionSequenceStartedAbilityEventNodeDataAccessor = chunk.GetBufferAccessor(ref ActionSequenceStartedAbilityEventNodeDataType);
			var EntityInstanceAccessor = chunk.GetBufferAccessor(ref EntityInstanceType);
			var ActionSequenceTimerEventNodeDataAccessor = chunk.GetBufferAccessor(ref ActionSequenceTimerEventNodeDataType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 19)
				{
					continue;
				}
				var _AbilityState = AbilityStateArray[c];
				var _DurationRef = new RefRW<TzarGames.GameCore.Abilities.Duration>(DurationArray, c);
				ref var _Duration = ref _DurationRef.ValueRW;
				var abilityEntity = EntityArray[c];
				var _AbilityOwner = AbilityOwnerArray[c];
				bool isOwner;
				if(IsServer)
				{
					isOwner = true;
				}
				else if(PlayerControllerLookup.TryGetComponent(_AbilityOwner.Value, out TzarGames.GameCore.PlayerController pc))
				{
					if(NetworkPlayerLookup.TryGetComponent(pc.Value, out TzarGames.MultiplayerKit.NetworkPlayer networkPlayer))
					{
						isOwner = networkPlayer.ItsMe;
					}
					else
					{
						isOwner = false;
					}
				}
				else
				{
					isOwner = false;
				}
				var abilityInterface = new AbilityInterface
				{
					Chunk = chunk,
					EntityIndex = c,
					IsServer = IsServer,
					IsOwner = isOwner,
				};
				var _VariableDataByteBuffer = VariableDataByteAccessor[c];
				var _EntityVariableDataBuffer = EntityVariableDataAccessor[c];
				var _ConstantEntityVariableDataBuffer = ConstantEntityVariableDataAccessor[c];
				var _ScriptVizStateRef = new RefRW<TzarGames.GameCore.ScriptViz.ScriptVizState>(ScriptVizStateArray, c);
				ref var _ScriptVizState = ref _ScriptVizStateRef.ValueRW;
				float deltaTime;
				if(DeltaTimeFromEntity.HasComponent(_AbilityOwner.Value))
				{
					deltaTime = DeltaTimeFromEntity[_AbilityOwner.Value].Value;
				}
				else
				{
					deltaTime = GlobalDeltaTime;
				}

				if(_AbilityState.Value == AbilityStates.Idle || _AbilityState.Value == AbilityStates.WaitingForValidation)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnIdleUpdate(ref _Duration);
					_ScriptVizAbilityJob.OnIdleUpdate(abilityEntity, unfilteredChunkIndex, in _AbilityOwner, Commands, in abilityInterface, ref _VariableDataByteBuffer, ref _EntityVariableDataBuffer, ref _abilityControl, in _ConstantEntityVariableDataBuffer, ref _ScriptVizState, in _ScriptVizCodeInfo, deltaTime);
				}

				var _ActionSequenceSharedDataRef = new RefRW<Arena.Abilities.ActionSequenceSharedData>(ActionSequenceSharedDataArray, c);
				ref var _ActionSequenceSharedData = ref _ActionSequenceSharedDataRef.ValueRW;
				var _AbilitySequenceActionBuffer = AbilitySequenceActionAccessor[c];
				var _HitQueryAbilityActionBuffer = HitQueryAbilityActionAccessor[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _ActionSequenceStartedAbilityEventNodeDataBuffer = ActionSequenceStartedAbilityEventNodeDataAccessor[c];
				var _EntityInstanceBuffer = EntityInstanceAccessor[c];
				var _ActionSequenceTimerEventNodeDataBuffer = ActionSequenceTimerEventNodeDataAccessor[c];

				var callWrapper = new ActionCallWrapper();
				callWrapper.Init();
				callWrapper._AbilityCylinderHitActionJob = _AbilityCylinderHitActionJob;
				callWrapper._ScriptVizSequenceStartActionJob = _ScriptVizSequenceStartActionJob;
				callWrapper._ScriptVizSequenceTimerEventActionJob = _ScriptVizSequenceTimerEventActionJob;
				callWrapper.unfilteredChunkIndex = unfilteredChunkIndex;
				callWrapper._AbilityOwner = _AbilityOwner;
				callWrapper._HitQueryAbilityActionBuffer = _HitQueryAbilityActionBuffer;
				callWrapper._LocalTransformRef = _LocalTransformRef;
				callWrapper.abilityInterface = abilityInterface;
				callWrapper.Commands = Commands;
				callWrapper.abilityEntity = abilityEntity;
				callWrapper._VariableDataByteBuffer = _VariableDataByteBuffer;
				callWrapper._EntityVariableDataBuffer = _EntityVariableDataBuffer;
				callWrapper._ConstantEntityVariableDataBuffer = _ConstantEntityVariableDataBuffer;
				callWrapper._ScriptVizStateRef = _ScriptVizStateRef;
				callWrapper._ActionSequenceStartedAbilityEventNodeDataBuffer = _ActionSequenceStartedAbilityEventNodeDataBuffer;
				callWrapper._ScriptVizCodeInfo = _ScriptVizCodeInfo;
				callWrapper._ActionSequenceSharedDataRef = _ActionSequenceSharedDataRef;
				callWrapper._DurationRef = _DurationRef;
				callWrapper.deltaTime = deltaTime;
				callWrapper._EntityInstanceBuffer = _EntityInstanceBuffer;
				callWrapper._ActionSequenceTimerEventNodeDataBuffer = _ActionSequenceTimerEventNodeDataBuffer;
				ref var actionCaller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCallWrapper, ActionCaller>(ref callWrapper);
				var _CopyOwnerCriticalToAbility = CopyOwnerCriticalToAbilityArray[c];
				var _CriticalDamageMultiplierRef = new RefRW<TzarGames.GameCore.CriticalDamageMultiplier>(CriticalDamageMultiplierArray, c);
				ref var _CriticalDamageMultiplier = ref _CriticalDamageMultiplierRef.ValueRW;
				var _CriticalDamageChanceRef = new RefRW<TzarGames.GameCore.CriticalDamageChance>(CriticalDamageChanceArray, c);
				ref var _CriticalDamageChance = ref _CriticalDamageChanceRef.ValueRW;
				var _CopyOwnerDamageToAbility = CopyOwnerDamageToAbilityArray[c];
				var _DamageRef = new RefRW<TzarGames.GameCore.Damage>(DamageArray, c);
				ref var _Damage = ref _DamageRef.ValueRW;
				var _CopyOwnerRadiusToAbility = CopyOwnerRadiusToAbilityArray[c];
				var _RadiusRef = new RefRW<TzarGames.GameCore.Radius>(RadiusArray, c);
				ref var _Radius = ref _RadiusRef.ValueRW;
				var _MinimalRadiusRef = new RefRW<TzarGames.GameCore.MinimalRadius>(MinimalRadiusArray, c);
				ref var _MinimalRadius = ref _MinimalRadiusRef.ValueRW;
				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _AddOwnerAttackVerticalOffsetAsTranslation = AddOwnerAttackVerticalOffsetAsTranslationArray[c];
				var _ConvertRotationToDirection = ConvertRotationToDirectionArray[c];
				var _DirectionRef = new RefRW<TzarGames.GameCore.Direction>(DirectionArray, c);
				ref var _Direction = ref _DirectionRef.ValueRW;
				var _MoveToTargetAbilityComponentDataRef = new RefRW<TzarGames.GameCore.Abilities.MoveToTargetAbilityComponentData>(MoveToTargetAbilityComponentDataArray, c);
				ref var _MoveToTargetAbilityComponentData = ref _MoveToTargetAbilityComponentDataRef.ValueRW;
				var _ModifyOwnerCharacteristicsRef = new RefRW<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(ModifyOwnerCharacteristicsArray, c);
				ref var _ModifyOwnerCharacteristics = ref _ModifyOwnerCharacteristicsRef.ValueRW;

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnStarted(ref _Duration);
					_ActionSequenceAbilityJob.OnStarted(ref _Duration, ref _ActionSequenceSharedData, ref _AbilitySequenceActionBuffer, ref actionCaller);
					_CopyOwnerCriticalToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerCriticalToAbility, ref _CriticalDamageMultiplier, ref _CriticalDamageChance);
					_CopyOwnerDamageToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerDamageToAbility, ref _Damage);
					_CopyOwnerRadiusToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerRadiusToAbility, ref _Radius, ref _MinimalRadius);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_AttackHeightJob.OnStarted(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_ConvertRotationToDirectionJob.OnStarted(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_MoveToTargetAbilityComponentJob.OnStarted(in _AbilityOwner, Commands, unfilteredChunkIndex, ref _MoveToTargetAbilityComponentData);
					_ModifyOwnerCharacteristicsJob.OnStarted(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_ScriptVizAbilityJob.OnStarted(abilityEntity, in _AbilityOwner, unfilteredChunkIndex, Commands, in abilityInterface, ref _abilityControl, ref _VariableDataByteBuffer, ref _EntityVariableDataBuffer, in _ConstantEntityVariableDataBuffer, ref _ScriptVizState, in _ScriptVizCodeInfo, deltaTime);

					if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
					{
						isJustStarted = true;
					}
				}

				if (isJustStarted)
				{
					_AbilityState.Value = AbilityStates.Running;
					AbilityStateArray[c] = _AbilityState;
					var eventEntity = Commands.CreateEntity(unfilteredChunkIndex, AbilityEventArchetype);
					Commands.SetComponent(unfilteredChunkIndex, eventEntity, new AbilityEvent { AbilityEntity = abilityEntity, EventType = AbilityEvents.Started });
				}

				var _CopyOwnerTransformToAbilityOnUpdate = CopyOwnerTransformToAbilityOnUpdateArray[c];

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_CopyOwnerTransformToAbilityOnUpdateJob.OnUpdate(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnUpdate, ref _LocalTransform);
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
					_AttackHeightJob.OnUpdate(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_ActionSequenceAbilityJob.OnUpdate(in _AbilityOwner, in _AbilityID, ref actionCaller, ref _ActionSequenceSharedData, ref _abilityControl, ref _Duration, ref _AbilitySequenceActionBuffer);
					_ConvertRotationToDirectionJob.OnUpdate(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_MoveToTargetAbilityComponentJob.OnUpdate(in _AbilityOwner, in _LocalTransform, in _Radius, ref _MoveToTargetAbilityComponentData, unfilteredChunkIndex, Commands);
					_ScriptVizAbilityJob.OnUpdate(abilityEntity, unfilteredChunkIndex, in _AbilityOwner, Commands, abilityInterface, ref _VariableDataByteBuffer, ref _EntityVariableDataBuffer, ref _abilityControl, in _ConstantEntityVariableDataBuffer, ref _ScriptVizState, in _ScriptVizCodeInfo, deltaTime);

					if(_abilityControl.StopRequest)
					{
						_AbilityState.Value = AbilityStates.Stopped;
					}

				}
				if(_AbilityState.Value == AbilityStates.Stopped)
				{
					var eventEntity = Commands.CreateEntity(unfilteredChunkIndex, AbilityEventArchetype);
					Commands.SetComponent(unfilteredChunkIndex, eventEntity, new AbilityEvent { AbilityEntity = abilityEntity, EventType = AbilityEvents.Stopped });
					AbilityControl _abilityControl = default;
					_MoveToTargetAbilityComponentJob.OnStopped(in _AbilityOwner, Commands, unfilteredChunkIndex);
					_DurationJob.OnStopped(ref _Duration);
					_ModifyOwnerCharacteristicsJob.OnStopped(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_ScriptVizAbilityJob.OnStopped(abilityEntity, unfilteredChunkIndex, in _AbilityOwner, Commands, in abilityInterface, ref _VariableDataByteBuffer, ref _EntityVariableDataBuffer, ref _abilityControl, in _ConstantEntityVariableDataBuffer, ref _ScriptVizState, in _ScriptVizCodeInfo, deltaTime);

					_AbilityState.Value = AbilityStates.Idle;
					AbilityStateArray[c] = _AbilityState;
				}
			}
		}
		[BurstCompile]
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct ActionCallWrapper
		{
			public ActionCaller Caller;
			public static readonly SharedStatic<FunctionPointer<ActionCaller.ActionCallDelegate>> ExecFunction = SharedStatic<FunctionPointer<ActionCaller.ActionCallDelegate>>.GetOrCreate<ActionCaller, ActionCallWrapper>();

			public TzarGames.GameCore.Abilities.AbilityCylinderHitActionJob _AbilityCylinderHitActionJob;
			public Arena.Abilities.ScriptVizSequenceStartActionJob _ScriptVizSequenceStartActionJob;
			public Arena.Abilities.ScriptVizSequenceTimerEventActionJob _ScriptVizSequenceTimerEventActionJob;

			public int unfilteredChunkIndex;
			public TzarGames.GameCore.Abilities.AbilityOwner _AbilityOwner;
			public DynamicBuffer<TzarGames.GameCore.Abilities.HitQueryAbilityAction> _HitQueryAbilityActionBuffer;
			public RefRW<Unity.Transforms.LocalTransform> _LocalTransformRef;
			public AbilityInterface abilityInterface;
			public EntityCommandBuffer.ParallelWriter Commands;
			public Entity abilityEntity;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.VariableDataByte> _VariableDataByteBuffer;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.EntityVariableData> _EntityVariableDataBuffer;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData> _ConstantEntityVariableDataBuffer;
			public RefRW<TzarGames.GameCore.ScriptViz.ScriptVizState> _ScriptVizStateRef;
			public DynamicBuffer<Arena.Abilities.ActionSequenceStartedAbilityEventNodeData> _ActionSequenceStartedAbilityEventNodeDataBuffer;
			public TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo _ScriptVizCodeInfo;
			public RefRW<Arena.Abilities.ActionSequenceSharedData> _ActionSequenceSharedDataRef;
			public RefRW<TzarGames.GameCore.Abilities.Duration> _DurationRef;
			public float deltaTime;
			public DynamicBuffer<TzarGames.GameCore.EntityInstance> _EntityInstanceBuffer;
			public DynamicBuffer<Arena.Abilities.ActionSequenceTimerEventNodeData> _ActionSequenceTimerEventNodeDataBuffer;
			public void Init()
			{
				Caller = new ActionCaller(ExecFunction.Data);
			}
			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(ActionCaller.ActionCallDelegate))]
			public static void ActionCallFunction(ref ActionCaller baseCaller, int callerId, byte actionId)
			{
				ref var caller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCaller, ActionCallWrapper>(ref baseCaller);
				foreach(var _HitQueryAbilityAction in caller._HitQueryAbilityActionBuffer)
				{
					if(_HitQueryAbilityAction.CallerId == callerId && _HitQueryAbilityAction.ActionId == actionId)
					{
						caller._AbilityCylinderHitActionJob.Execute(caller.unfilteredChunkIndex, in caller._AbilityOwner, _HitQueryAbilityAction, in caller._LocalTransformRef.ValueRW, caller.abilityInterface, in caller.Commands);
						break;
					}
				}
				foreach(var _ActionSequenceStartedAbilityEventNodeData in caller._ActionSequenceStartedAbilityEventNodeDataBuffer)
				{
					if(_ActionSequenceStartedAbilityEventNodeData.CallerId == callerId && _ActionSequenceStartedAbilityEventNodeData.ActionId == actionId)
					{
						caller._ScriptVizSequenceStartActionJob.Execute(caller.abilityEntity, caller.unfilteredChunkIndex, in caller._AbilityOwner, caller.Commands, ref caller._VariableDataByteBuffer, ref caller._EntityVariableDataBuffer, ref caller._ConstantEntityVariableDataBuffer, ref caller._ScriptVizStateRef.ValueRW, in _ActionSequenceStartedAbilityEventNodeData, in caller._ScriptVizCodeInfo, in caller._ActionSequenceSharedDataRef.ValueRW, in caller._DurationRef.ValueRW, caller.deltaTime);
						break;
					}
				}
				foreach(var _ActionSequenceTimerEventNodeData in caller._ActionSequenceTimerEventNodeDataBuffer)
				{
					if(_ActionSequenceTimerEventNodeData.CallerId == callerId && _ActionSequenceTimerEventNodeData.ActionId == actionId)
					{
						caller._ScriptVizSequenceTimerEventActionJob.Execute(caller.abilityEntity, caller.unfilteredChunkIndex, in caller._AbilityOwner, caller.Commands, ref caller._VariableDataByteBuffer, ref caller._EntityVariableDataBuffer, ref caller._ConstantEntityVariableDataBuffer, in caller._EntityInstanceBuffer, ref caller._ScriptVizStateRef.ValueRW, in _ActionSequenceTimerEventNodeData, in caller._ScriptVizCodeInfo, in caller._ActionSequenceSharedDataRef.ValueRW, in caller._LocalTransformRef.ValueRW, caller.deltaTime);
						break;
					}
				}
			}
		}

	}
	static class Meleeattackability_19_Initializator
	{
		static System.Type jobType = typeof(Meleeattackability_19_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Meleeattackability_19_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Meleeattackability_19_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Meleeattackability_19_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Client");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Meleeattackability_19_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Meleeattackability_19_System : ISystem
	{
		EntityQuery query;
		Meleeattackability_19_AbilityJob job;

		public void OnCreate(ref SystemState state)
		{
			var entityQueryDesc = new EntityQueryDesc()
			{
				All = new []
				{
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityState>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityID>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.Duration>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityOwner>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.VariableDataByte>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.EntityVariableData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.ScriptVizState>(),
					ComponentType.ReadOnly<TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(),
					ComponentType.ReadWrite<Unity.Transforms.LocalTransform>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(),
					ComponentType.ReadWrite<Arena.Abilities.ActionSequenceSharedData>(),
					ComponentType.ReadWrite<Arena.Abilities.AbilitySequenceAction>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Direction>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Radius>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.MoveToTargetAbilityComponentData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.CriticalDamageMultiplier>(),
					ComponentType.ReadWrite<TzarGames.GameCore.CriticalDamageChance>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Damage>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.MinimalRadius>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.HitQueryAbilityAction>(),
					ComponentType.ReadOnly<Arena.Abilities.ActionSequenceStartedAbilityEventNodeData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.EntityInstance>(),
					ComponentType.ReadOnly<Arena.Abilities.ActionSequenceTimerEventNodeData>(),
				}
			};
			query = state.GetEntityQuery(entityQueryDesc);
			job = createJob(ref state);
		}
		public void OnDestroy(ref SystemState state)
		{
		}
		public void OnUpdate(ref SystemState state)
		{
			updateJob(ref state);
			query.CompleteDependency();
			job.Run(query);
		}
		Meleeattackability_19_AbilityJob createJob(ref SystemState state)
		{
			var job = new Meleeattackability_19_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.NetworkPlayerLookup = SystemAPI.GetComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer>(true);
			job.PlayerControllerLookup = SystemAPI.GetComponentLookup<TzarGames.GameCore.PlayerController>(true);
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.EntityType = state.GetEntityTypeHandle();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.VariableDataByteType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.VariableDataByte>();
			job.EntityVariableDataType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.EntityVariableData>();
			job.ConstantEntityVariableDataType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData>();
			job.ScriptVizStateType = state.GetComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizState>();
			job.ScriptVizCodeInfoType = state.GetSharedComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo>();
			job.CopyOwnerTransformToAbilityOnUpdateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.AddOwnerAttackVerticalOffsetAsTranslationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(true);
			job.ActionSequenceSharedDataType = state.GetComponentTypeHandle<Arena.Abilities.ActionSequenceSharedData>();
			job.AbilitySequenceActionType = state.GetBufferTypeHandle<Arena.Abilities.AbilitySequenceAction>();
			job.ConvertRotationToDirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(true);
			job.DirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Direction>();
			job.RadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.Radius>();
			job.MoveToTargetAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.MoveToTargetAbilityComponentData>();
			job.CopyOwnerCriticalToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility>(true);
			job.CriticalDamageMultiplierType = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier>();
			job.CriticalDamageChanceType = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance>();
			job.CopyOwnerDamageToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(true);
			job.DamageType = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>();
			job.CopyOwnerRadiusToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(true);
			job.MinimalRadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.MinimalRadius>();
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.ModifyOwnerCharacteristicsType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>();
			job.HitQueryAbilityActionType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.HitQueryAbilityAction>(true);
			job.ActionSequenceStartedAbilityEventNodeDataType = state.GetBufferTypeHandle<Arena.Abilities.ActionSequenceStartedAbilityEventNodeData>(true);
			job.EntityInstanceType = state.GetBufferTypeHandle<TzarGames.GameCore.EntityInstance>(true);
			job.ActionSequenceTimerEventNodeDataType = state.GetBufferTypeHandle<Arena.Abilities.ActionSequenceTimerEventNodeData>(true);

			var ComponentLookupCharacterInputs = state.GetComponentLookup<TzarGames.GameCore.CharacterInputs>(true);
			var ComponentLookupTarget = state.GetComponentLookup<TzarGames.GameCore.Target>(true);
			var ComponentLookupTargetPosition = state.GetComponentLookup<TzarGames.GameCore.TargetPosition>(true);
			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var ComponentLookupRadius = state.GetComponentLookup<TzarGames.GameCore.Radius>(true);
			var ComponentTypeHandleRadius = state.GetComponentTypeHandle<TzarGames.GameCore.Radius>(true);
			var ComponentLookupPlayerInput = state.GetComponentLookup<Arena.PlayerInput>(true);
			var ComponentLookupAttackVerticalOffset = state.GetComponentLookup<TzarGames.GameCore.AttackVerticalOffset>(true);
			var ComponentLookupCriticalDamageMultiplier = state.GetComponentLookup<TzarGames.GameCore.CriticalDamageMultiplier>(true);
			var ComponentLookupCriticalDamageChance = state.GetComponentLookup<TzarGames.GameCore.CriticalDamageChance>(true);
			var ComponentLookupDamage = state.GetComponentLookup<TzarGames.GameCore.Damage>(true);
			var ComponentLookupAttackRadius = state.GetComponentLookup<TzarGames.GameCore.AttackRadius>(true);
			var BufferLookupSpeedModificator = state.GetBufferLookup<TzarGames.GameCore.SpeedModificator>(true);
			var ComponentTypeHandleAngle = state.GetComponentTypeHandle<TzarGames.GameCore.Angle>(true);
			var ComponentTypeHandleLocalTransform = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>(true);
			var ComponentTypeHandleMinimalRadius = state.GetComponentTypeHandle<TzarGames.GameCore.MinimalRadius>(true);
			var ComponentTypeHandleDamage = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>(true);
			var ComponentTypeHandleHeight = state.GetComponentTypeHandle<TzarGames.GameCore.Height>(true);
			var ComponentTypeHandleInstigator = state.GetComponentTypeHandle<TzarGames.GameCore.Instigator>(true);
			var ComponentTypeHandleCriticalDamageChance = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance>(true);
			var ComponentTypeHandleCriticalDamageMultiplier = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier>(true);
			var BufferTypeHandleEntityInstance = state.GetBufferTypeHandle<TzarGames.GameCore.EntityInstance>();
			var ComponentTypeHandleWeaponSurface = state.GetComponentTypeHandle<TzarGames.GameCore.WeaponSurface>(true);
			var ComponentTypeHandleDuration = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>(true);
			var BufferTypeHandleOnAbilityStartEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStartEventCommandData>(true);
			var BufferTypeHandleOnAbilityStopEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStopEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateIdleEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateIdleEventCommandData>(true);

			job._MoveToTargetAbilityComponentJob.InputFromEntity = ComponentLookupCharacterInputs;
			job._MoveToTargetAbilityComponentJob.TargetFromEntity = ComponentLookupTarget;
			job._MoveToTargetAbilityComponentJob.TargetPositionFromEntity = ComponentLookupTargetPosition;
			job._MoveToTargetAbilityComponentJob.TransformFromEntity = ComponentLookupLocalTransform;
			job._MoveToTargetAbilityComponentJob.RadiusFromEntity = ComponentLookupRadius;
			job._MoveToTargetAbilityComponentJob.RadiusType = ComponentTypeHandleRadius;

			job._ActionSequenceAbilityJob.PlayerInputFromEntity = ComponentLookupPlayerInput;

			job._AttackHeightJob.MeleeAttackerFromEntity = ComponentLookupAttackVerticalOffset;


			job._CopyOwnerCriticalToAbilityJob.MultiplierFromEntity = ComponentLookupCriticalDamageMultiplier;
			job._CopyOwnerCriticalToAbilityJob.CritChanceFromEntity = ComponentLookupCriticalDamageChance;

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity = ComponentLookupDamage;

			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity = ComponentLookupAttackRadius;
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity = ComponentLookupRadius;

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity = BufferLookupSpeedModificator;
			job._ModifyOwnerCharacteristicsJob.Initialize(ref state);

			job._AbilityCylinderHitActionJob.AngleType = ComponentTypeHandleAngle;
			job._AbilityCylinderHitActionJob.TransformType = ComponentTypeHandleLocalTransform;
			job._AbilityCylinderHitActionJob.RadiusType = ComponentTypeHandleRadius;
			job._AbilityCylinderHitActionJob.MinRadiusType = ComponentTypeHandleMinimalRadius;
			job._AbilityCylinderHitActionJob.DamageType = ComponentTypeHandleDamage;
			job._AbilityCylinderHitActionJob.HeightType = ComponentTypeHandleHeight;
			job._AbilityCylinderHitActionJob.InstigatorType = ComponentTypeHandleInstigator;
			job._AbilityCylinderHitActionJob.CritChanceType = ComponentTypeHandleCriticalDamageChance;
			job._AbilityCylinderHitActionJob.CritMultType = ComponentTypeHandleCriticalDamageMultiplier;
			job._AbilityCylinderHitActionJob.EntityInstanceArrayType = BufferTypeHandleEntityInstance;
			job._AbilityCylinderHitActionJob.Targets = ComponentLookupTarget;
			job._AbilityCylinderHitActionJob.SurfaceType = ComponentTypeHandleWeaponSurface;



			job._ScriptVizAbilityJob.DurationType = ComponentTypeHandleDuration;
			job._ScriptVizAbilityJob.StartEventType = BufferTypeHandleOnAbilityStartEventCommandData;
			job._ScriptVizAbilityJob.StopEventType = BufferTypeHandleOnAbilityStopEventCommandData;
			job._ScriptVizAbilityJob.UpdateEventType = BufferTypeHandleOnAbilityUpdateEventCommandData;
			job._ScriptVizAbilityJob.UpdateIdleEventType = BufferTypeHandleOnAbilityUpdateIdleEventCommandData;

			return job;
		}
		void updateJob(ref SystemState state)
		{
			var cmdSingleton = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
			job.Commands = cmdSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.IsServer = abilitySystem.IsServer;
			job.NetworkPlayerLookup.Update(ref state);
			job.PlayerControllerLookup.Update(ref state);
			job.GlobalDeltaTime = SystemAPI.Time.DeltaTime;
			job.DeltaTimeFromEntity.Update(ref state);
			job.AbilityStateType.Update(ref state);
			job.AbilityIDType.Update(ref state);
			job.DurationType.Update(ref state);
			job.EntityType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.VariableDataByteType.Update(ref state);
			job.EntityVariableDataType.Update(ref state);
			job.ConstantEntityVariableDataType.Update(ref state);
			job.ScriptVizStateType.Update(ref state);
			job.ScriptVizCodeInfoType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnUpdateType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.AddOwnerAttackVerticalOffsetAsTranslationType.Update(ref state);
			job.ActionSequenceSharedDataType.Update(ref state);
			job.AbilitySequenceActionType.Update(ref state);
			job.ConvertRotationToDirectionType.Update(ref state);
			job.DirectionType.Update(ref state);
			job.RadiusType.Update(ref state);
			job.MoveToTargetAbilityComponentDataType.Update(ref state);
			job.CopyOwnerCriticalToAbilityType.Update(ref state);
			job.CriticalDamageMultiplierType.Update(ref state);
			job.CriticalDamageChanceType.Update(ref state);
			job.CopyOwnerDamageToAbilityType.Update(ref state);
			job.DamageType.Update(ref state);
			job.CopyOwnerRadiusToAbilityType.Update(ref state);
			job.MinimalRadiusType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.ModifyOwnerCharacteristicsType.Update(ref state);
			job.HitQueryAbilityActionType.Update(ref state);
			job.ActionSequenceStartedAbilityEventNodeDataType.Update(ref state);
			job.EntityInstanceType.Update(ref state);
			job.ActionSequenceTimerEventNodeDataType.Update(ref state);


			job._MoveToTargetAbilityComponentJob.InputFromEntity.Update(ref state);
			job._MoveToTargetAbilityComponentJob.TargetFromEntity.Update(ref state);
			job._MoveToTargetAbilityComponentJob.TargetPositionFromEntity.Update(ref state);
			job._MoveToTargetAbilityComponentJob.TransformFromEntity.Update(ref state);
			job._MoveToTargetAbilityComponentJob.RadiusFromEntity.Update(ref state);
			job._MoveToTargetAbilityComponentJob.RadiusType.Update(ref state);

			job._ActionSequenceAbilityJob.PlayerInputFromEntity.Update(ref state);

			job._AttackHeightJob.MeleeAttackerFromEntity.Update(ref state);


			job._CopyOwnerCriticalToAbilityJob.MultiplierFromEntity.Update(ref state);
			job._CopyOwnerCriticalToAbilityJob.CritChanceFromEntity.Update(ref state);

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity.Update(ref state);

			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity.Update(ref state);
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity.Update(ref state);


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity.Update(ref state);

			job._AbilityCylinderHitActionJob.AngleType.Update(ref state);
			job._AbilityCylinderHitActionJob.TransformType.Update(ref state);
			job._AbilityCylinderHitActionJob.RadiusType.Update(ref state);
			job._AbilityCylinderHitActionJob.MinRadiusType.Update(ref state);
			job._AbilityCylinderHitActionJob.DamageType.Update(ref state);
			job._AbilityCylinderHitActionJob.HeightType.Update(ref state);
			job._AbilityCylinderHitActionJob.InstigatorType.Update(ref state);
			job._AbilityCylinderHitActionJob.CritChanceType.Update(ref state);
			job._AbilityCylinderHitActionJob.CritMultType.Update(ref state);
			job._AbilityCylinderHitActionJob.EntityInstanceArrayType.Update(ref state);
			job._AbilityCylinderHitActionJob.Targets.Update(ref state);
			job._AbilityCylinderHitActionJob.SurfaceType.Update(ref state);



			job._ScriptVizAbilityJob.DurationType.Update(ref state);
			job._ScriptVizAbilityJob.StartEventType.Update(ref state);
			job._ScriptVizAbilityJob.StopEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateIdleEventType.Update(ref state);

		}
	}
}
