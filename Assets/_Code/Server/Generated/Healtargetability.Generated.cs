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
	public struct Healtargetability_147_AbilityJob : IJobChunk
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
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown> AbilityCooldownType;
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
		public BufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerAction> AbilityTimerActionType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData> AbilityTimerSharedDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData> AbilityTimerDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics> ModifyOwnerCharacteristicsType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData> SetOwnerAsTargetAbilityDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Target> TargetType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<TzarGames.GameCore.Abilities.ScriptVizAbilityTimerEventNodeData> ScriptVizAbilityTimerEventNodeDataType;

		public TzarGames.GameCore.Abilities.AbilityCooldownJob _AbilityCooldownJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.ModifyOwnerCharacteristicsJob _ModifyOwnerCharacteristicsJob;
		public TzarGames.GameCore.Abilities.SetTargetAbilityJob _SetTargetAbilityJob;
		public TzarGames.GameCore.Abilities.ScriptVizTimerEventActionJob _ScriptVizTimerEventActionJob;
		public TzarGames.GameCore.Abilities.TimerEventAbilityComponentJob _TimerEventAbilityComponentJob;
		public TzarGames.GameCore.Abilities.ScriptVizAbilityJob _ScriptVizAbilityJob;


		bool Validate(in TzarGames.GameCore.Abilities.AbilityCooldown _AbilityCooldown)
		{
			if(_AbilityCooldownJob.OnValidate(in _AbilityCooldown) == false)
			{
				return false;
			}
			return true;
		}
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			var AbilityStateArray = chunk.GetNativeArray(ref AbilityStateType);
			var AbilityIDArray = chunk.GetNativeArray(ref AbilityIDType);
			var AbilityCooldownArray = chunk.GetNativeArray(ref AbilityCooldownType);
			var DurationArray = chunk.GetNativeArray(ref DurationType);
			var EntityArray = chunk.GetNativeArray(EntityType);
			var AbilityOwnerArray = chunk.GetNativeArray(ref AbilityOwnerType);
			var VariableDataByteAccessor = chunk.GetBufferAccessor(ref VariableDataByteType);
			var EntityVariableDataAccessor = chunk.GetBufferAccessor(ref EntityVariableDataType);
			var ConstantEntityVariableDataAccessor = chunk.GetBufferAccessor(ref ConstantEntityVariableDataType);
			var ScriptVizStateArray = chunk.GetNativeArray(ref ScriptVizStateType);
			var _ScriptVizCodeInfo = chunk.GetSharedComponent(ScriptVizCodeInfoType);
			var AbilityTimerActionAccessor = chunk.GetBufferAccessor(ref AbilityTimerActionType);
			var AbilityTimerSharedDataArray = chunk.GetNativeArray(ref AbilityTimerSharedDataType);
			var AbilityTimerDataArray = chunk.GetNativeArray(ref AbilityTimerDataType);
			var ModifyOwnerCharacteristicsArray = chunk.GetNativeArray(ref ModifyOwnerCharacteristicsType);
			var SetOwnerAsTargetAbilityDataArray = chunk.GetNativeArray(ref SetOwnerAsTargetAbilityDataType);
			var TargetArray = chunk.GetNativeArray(ref TargetType);
			var ScriptVizAbilityTimerEventNodeDataAccessor = chunk.GetBufferAccessor(ref ScriptVizAbilityTimerEventNodeDataType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 147)
				{
					continue;
				}
				var _AbilityState = AbilityStateArray[c];
				var _AbilityOwner = AbilityOwnerArray[c];

				float deltaTime;
				if(DeltaTimeFromEntity.HasComponent(_AbilityOwner.Value))
				{
					deltaTime = DeltaTimeFromEntity[_AbilityOwner.Value].Value;
				}
				else
				{
					deltaTime = GlobalDeltaTime;
				}
				var _AbilityCooldownRef = new RefRW<TzarGames.GameCore.Abilities.AbilityCooldown>(AbilityCooldownArray, c);
				ref var _AbilityCooldown = ref _AbilityCooldownRef.ValueRW;
				var _DurationRef = new RefRW<TzarGames.GameCore.Abilities.Duration>(DurationArray, c);
				ref var _Duration = ref _DurationRef.ValueRW;
				var abilityEntity = EntityArray[c];
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

				if(_AbilityState.Value == AbilityStates.Idle || _AbilityState.Value == AbilityStates.WaitingForValidation)
				{
					AbilityControl _abilityControl = default;
					_AbilityCooldownJob.OnIdleUpdate(deltaTime, ref _AbilityCooldown);
					_DurationJob.OnIdleUpdate(ref _Duration);
					_ScriptVizAbilityJob.OnIdleUpdate(abilityEntity, unfilteredChunkIndex, in _AbilityOwner, Commands, in abilityInterface, ref _VariableDataByteBuffer, ref _EntityVariableDataBuffer, ref _abilityControl, in _ConstantEntityVariableDataBuffer, ref _ScriptVizState, in _ScriptVizCodeInfo, deltaTime);
				}

				if(_AbilityState.Value == AbilityStates.WaitingForValidation)
				{
					var validateResult = Validate(in _AbilityCooldown);
					if(validateResult)
					{
						_AbilityState.Value = AbilityStates.ValidatedAndWaitingForStart;
					}
					else
					{
						_AbilityState.Value = AbilityStates.Idle;
						AbilityStateArray[c] = _AbilityState;
						continue;
					}
				}

				var _AbilityTimerActionBuffer = AbilityTimerActionAccessor[c];
				var _AbilityTimerSharedData = AbilityTimerSharedDataArray[c];
				var _AbilityTimerDataRef = new RefRW<TzarGames.GameCore.Abilities.AbilityTimerData>(AbilityTimerDataArray, c);
				ref var _AbilityTimerData = ref _AbilityTimerDataRef.ValueRW;
				var _ModifyOwnerCharacteristicsRef = new RefRW<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(ModifyOwnerCharacteristicsArray, c);
				ref var _ModifyOwnerCharacteristics = ref _ModifyOwnerCharacteristicsRef.ValueRW;
				var _SetOwnerAsTargetAbilityData = SetOwnerAsTargetAbilityDataArray[c];
				var _TargetRef = new RefRW<TzarGames.GameCore.Target>(TargetArray, c);
				ref var _Target = ref _TargetRef.ValueRW;

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnStarted(ref _Duration);
					_TimerEventAbilityComponentJob.OnStarted(ref _AbilityTimerActionBuffer, in _AbilityTimerSharedData, ref _AbilityTimerData);
					_AbilityCooldownJob.OnStarted(ref _AbilityCooldown);
					_ModifyOwnerCharacteristicsJob.OnStarted(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_SetTargetAbilityJob.OnStarted(in _AbilityOwner, in _SetOwnerAsTargetAbilityData, ref _Target);
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

				var _ScriptVizAbilityTimerEventNodeDataBuffer = ScriptVizAbilityTimerEventNodeDataAccessor[c];

				var callWrapper = new ActionCallWrapper();
				callWrapper.Init();
				callWrapper._ScriptVizTimerEventActionJob = _ScriptVizTimerEventActionJob;
				callWrapper.abilityEntity = abilityEntity;
				callWrapper.unfilteredChunkIndex = unfilteredChunkIndex;
				callWrapper._AbilityOwner = _AbilityOwner;
				callWrapper.Commands = Commands;
				callWrapper._VariableDataByteBuffer = _VariableDataByteBuffer;
				callWrapper._EntityVariableDataBuffer = _EntityVariableDataBuffer;
				callWrapper._ConstantEntityVariableDataBuffer = _ConstantEntityVariableDataBuffer;
				callWrapper._ScriptVizStateRef = _ScriptVizStateRef;
				callWrapper._ScriptVizAbilityTimerEventNodeDataBuffer = _ScriptVizAbilityTimerEventNodeDataBuffer;
				callWrapper._ScriptVizCodeInfo = _ScriptVizCodeInfo;
				callWrapper.deltaTime = deltaTime;
				ref var actionCaller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCallWrapper, ActionCaller>(ref callWrapper);

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
					_AbilityCooldownJob.OnUpdate(deltaTime, ref _AbilityCooldown);
					_TimerEventAbilityComponentJob.OnUpdate(ref _AbilityTimerActionBuffer, ref actionCaller, in _Duration, in _AbilityTimerSharedData, in abilityInterface, ref _AbilityTimerData);
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

			public TzarGames.GameCore.Abilities.ScriptVizTimerEventActionJob _ScriptVizTimerEventActionJob;

			public Entity abilityEntity;
			public int unfilteredChunkIndex;
			public TzarGames.GameCore.Abilities.AbilityOwner _AbilityOwner;
			public EntityCommandBuffer.ParallelWriter Commands;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.VariableDataByte> _VariableDataByteBuffer;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.EntityVariableData> _EntityVariableDataBuffer;
			public DynamicBuffer<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData> _ConstantEntityVariableDataBuffer;
			public RefRW<TzarGames.GameCore.ScriptViz.ScriptVizState> _ScriptVizStateRef;
			public DynamicBuffer<TzarGames.GameCore.Abilities.ScriptVizAbilityTimerEventNodeData> _ScriptVizAbilityTimerEventNodeDataBuffer;
			public TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo _ScriptVizCodeInfo;
			public float deltaTime;
			public void Init()
			{
				Caller = new ActionCaller(ExecFunction.Data);
			}
			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(ActionCaller.ActionCallDelegate))]
			public static void ActionCallFunction(ref ActionCaller baseCaller, int callerId, byte actionId)
			{
				ref var caller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCaller, ActionCallWrapper>(ref baseCaller);
				foreach(var _ScriptVizAbilityTimerEventNodeData in caller._ScriptVizAbilityTimerEventNodeDataBuffer)
				{
					if(_ScriptVizAbilityTimerEventNodeData.CallerId == callerId && _ScriptVizAbilityTimerEventNodeData.ActionId == actionId)
					{
						caller._ScriptVizTimerEventActionJob.Execute(caller.abilityEntity, caller.unfilteredChunkIndex, in caller._AbilityOwner, caller.Commands, ref caller._VariableDataByteBuffer, ref caller._EntityVariableDataBuffer, in caller._ConstantEntityVariableDataBuffer, ref caller._ScriptVizStateRef.ValueRW, in _ScriptVizAbilityTimerEventNodeData, in caller._ScriptVizCodeInfo, caller.deltaTime);
						break;
					}
				}
			}
		}

	}
	static class Healtargetability_147_Initializator
	{
		static System.Type jobType = typeof(Healtargetability_147_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Healtargetability_147_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Healtargetability_147_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Healtargetability_147_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Server");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Healtargetability_147_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Healtargetability_147_System : ISystem
	{
		EntityQuery query;
		Healtargetability_147_AbilityJob job;

		public void OnCreate(ref SystemState state)
		{
			var entityQueryDesc = new EntityQueryDesc()
			{
				All = new []
				{
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityState>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityID>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityCooldown>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.Duration>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityOwner>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.VariableDataByte>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.EntityVariableData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.ScriptViz.ScriptVizState>(),
					ComponentType.ReadOnly<TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerAction>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Target>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ScriptVizAbilityTimerEventNodeData>(),
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
		Healtargetability_147_AbilityJob createJob(ref SystemState state)
		{
			var job = new Healtargetability_147_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.NetworkPlayerLookup = SystemAPI.GetComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer>(true);
			job.PlayerControllerLookup = SystemAPI.GetComponentLookup<TzarGames.GameCore.PlayerController>(true);
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.AbilityCooldownType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown>();
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.EntityType = state.GetEntityTypeHandle();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.VariableDataByteType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.VariableDataByte>();
			job.EntityVariableDataType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.EntityVariableData>();
			job.ConstantEntityVariableDataType = state.GetBufferTypeHandle<TzarGames.GameCore.ScriptViz.ConstantEntityVariableData>(true);
			job.ScriptVizStateType = state.GetComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizState>();
			job.ScriptVizCodeInfoType = state.GetSharedComponentTypeHandle<TzarGames.GameCore.ScriptViz.ScriptVizCodeInfo>();
			job.AbilityTimerActionType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerAction>();
			job.AbilityTimerSharedDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(true);
			job.AbilityTimerDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData>();
			job.ModifyOwnerCharacteristicsType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>();
			job.SetOwnerAsTargetAbilityDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData>(true);
			job.TargetType = state.GetComponentTypeHandle<TzarGames.GameCore.Target>();
			job.ScriptVizAbilityTimerEventNodeDataType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.ScriptVizAbilityTimerEventNodeData>(true);

			var BufferLookupSpeedModificator = state.GetBufferLookup<TzarGames.GameCore.SpeedModificator>(true);
			var ComponentLookupTarget = state.GetComponentLookup<TzarGames.GameCore.Target>(true);
			var ComponentTypeHandleDuration = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>(true);
			var BufferTypeHandleOnAbilityStartEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStartEventCommandData>(true);
			var BufferTypeHandleOnAbilityStopEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStopEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateIdleEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateIdleEventCommandData>(true);
			var BufferTypeHandleEntityInstance = state.GetBufferTypeHandle<TzarGames.GameCore.EntityInstance>(true);



			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity = BufferLookupSpeedModificator;
			job._ModifyOwnerCharacteristicsJob.Initialize(ref state);

			job._SetTargetAbilityJob.TargetFromEntity = ComponentLookupTarget;



			job._ScriptVizAbilityJob.DurationType = ComponentTypeHandleDuration;
			job._ScriptVizAbilityJob.StartEventType = BufferTypeHandleOnAbilityStartEventCommandData;
			job._ScriptVizAbilityJob.StopEventType = BufferTypeHandleOnAbilityStopEventCommandData;
			job._ScriptVizAbilityJob.UpdateEventType = BufferTypeHandleOnAbilityUpdateEventCommandData;
			job._ScriptVizAbilityJob.UpdateIdleEventType = BufferTypeHandleOnAbilityUpdateIdleEventCommandData;
			job._ScriptVizAbilityJob.EntityInstanceArrayType = BufferTypeHandleEntityInstance;

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
			job.AbilityCooldownType.Update(ref state);
			job.DurationType.Update(ref state);
			job.EntityType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.VariableDataByteType.Update(ref state);
			job.EntityVariableDataType.Update(ref state);
			job.ConstantEntityVariableDataType.Update(ref state);
			job.ScriptVizStateType.Update(ref state);
			job.ScriptVizCodeInfoType.Update(ref state);
			job.AbilityTimerActionType.Update(ref state);
			job.AbilityTimerSharedDataType.Update(ref state);
			job.AbilityTimerDataType.Update(ref state);
			job.ModifyOwnerCharacteristicsType.Update(ref state);
			job.SetOwnerAsTargetAbilityDataType.Update(ref state);
			job.TargetType.Update(ref state);
			job.ScriptVizAbilityTimerEventNodeDataType.Update(ref state);




			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity.Update(ref state);

			job._SetTargetAbilityJob.TargetFromEntity.Update(ref state);



			job._ScriptVizAbilityJob.DurationType.Update(ref state);
			job._ScriptVizAbilityJob.StartEventType.Update(ref state);
			job._ScriptVizAbilityJob.StopEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateIdleEventType.Update(ref state);
			job._ScriptVizAbilityJob.EntityInstanceArrayType.Update(ref state);

		}
	}
}
