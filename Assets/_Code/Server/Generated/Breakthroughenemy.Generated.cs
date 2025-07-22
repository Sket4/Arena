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
	public struct Breakthroughenemy_89_AbilityJob : IJobChunk
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
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate> CopyOwnerTransformToAbilityOnUpdateType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<Unity.Transforms.LocalTransform> LocalTransformType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.MoveAbilityComponentData> MoveAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility> CopyOwnerDamageToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Damage> DamageType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart> CopyOwnerTransformToAbilityOnStartType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationAbility> ConvertOwnerInputToRotationAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.HitQuery> HitQueryType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Instigator> InstigatorType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics> ModifyOwnerCharacteristicsType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.HitFlagElement> HitFlagElementType;

		public TzarGames.GameCore.HitQueryAbilityComponentJob _HitQueryAbilityComponentJob;
		public TzarGames.GameCore.Abilities.AbilityCooldownJob _AbilityCooldownJob;
		public TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationOnStartAbilityJob _ConvertOwnerInputToRotationOnStartAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerDamageToAbilityJob _CopyOwnerDamageToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStartJob _CopyOwnerTransformToAbilityOnStartJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdateJob _CopyOwnerTransformToAbilityOnUpdateJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.ModifyOwnerCharacteristicsJob _ModifyOwnerCharacteristicsJob;
		public TzarGames.GameCore.Abilities.MoveAbilityComponentJob _MoveAbilityComponentJob;
		public TzarGames.GameCore.HitFlagAbilityComponentJob _HitFlagAbilityComponentJob;
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
			var CopyOwnerTransformToAbilityOnUpdateArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnUpdateType);
			var LocalTransformArray = chunk.GetNativeArray(ref LocalTransformType);
			var MoveAbilityComponentDataArray = chunk.GetNativeArray(ref MoveAbilityComponentDataType);
			var CopyOwnerDamageToAbilityArray = chunk.GetNativeArray(ref CopyOwnerDamageToAbilityType);
			var DamageArray = chunk.GetNativeArray(ref DamageType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var ConvertOwnerInputToRotationAbilityArray = chunk.GetNativeArray(ref ConvertOwnerInputToRotationAbilityType);
			var HitQueryArray = chunk.GetNativeArray(ref HitQueryType);
			var InstigatorArray = chunk.GetNativeArray(ref InstigatorType);
			var ModifyOwnerCharacteristicsArray = chunk.GetNativeArray(ref ModifyOwnerCharacteristicsType);
			var HitFlagElementAccessor = chunk.GetBufferAccessor(ref HitFlagElementType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 89)
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

				var _CopyOwnerDamageToAbility = CopyOwnerDamageToAbilityArray[c];
				var _DamageRef = new RefRW<TzarGames.GameCore.Damage>(DamageArray, c);
				ref var _Damage = ref _DamageRef.ValueRW;
				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _ConvertOwnerInputToRotationAbility = ConvertOwnerInputToRotationAbilityArray[c];
				var _HitQueryRef = new RefRW<TzarGames.GameCore.HitQuery>(HitQueryArray, c);
				ref var _HitQuery = ref _HitQueryRef.ValueRW;
				var _InstigatorRef = new RefRW<TzarGames.GameCore.Instigator>(InstigatorArray, c);
				ref var _Instigator = ref _InstigatorRef.ValueRW;
				var _ModifyOwnerCharacteristicsRef = new RefRW<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(ModifyOwnerCharacteristicsArray, c);
				ref var _ModifyOwnerCharacteristics = ref _ModifyOwnerCharacteristicsRef.ValueRW;
				var _MoveAbilityComponentDataRef = new RefRW<TzarGames.GameCore.Abilities.MoveAbilityComponentData>(MoveAbilityComponentDataArray, c);
				ref var _MoveAbilityComponentData = ref _MoveAbilityComponentDataRef.ValueRW;

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnStarted(ref _Duration);
					_CopyOwnerDamageToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerDamageToAbility, ref _Damage);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_ConvertOwnerInputToRotationOnStartAbilityJob.OnStarted(in _AbilityOwner, in _ConvertOwnerInputToRotationAbility, ref _LocalTransform);
					_HitQueryAbilityComponentJob.OnStarted(in _AbilityOwner, ref _HitQuery, ref _Instigator);
					_AbilityCooldownJob.OnStarted(ref _AbilityCooldown);
					_ModifyOwnerCharacteristicsJob.OnStarted(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_MoveAbilityComponentJob.OnStarted(in _AbilityOwner, ref _MoveAbilityComponentData, _LocalTransform, unfilteredChunkIndex, Commands);
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
					_AbilityCooldownJob.OnUpdate(deltaTime, ref _AbilityCooldown);
					_MoveAbilityComponentJob.OnUpdate(in _AbilityOwner, unfilteredChunkIndex, Commands, ref _MoveAbilityComponentData, ref _abilityControl);
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
					var _HitFlagElementBuffer = HitFlagElementAccessor[c];

					_HitQueryAbilityComponentJob.OnStopped(ref _HitQuery);
					_DurationJob.OnStopped(ref _Duration);
					_ModifyOwnerCharacteristicsJob.OnStopped(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_HitFlagAbilityComponentJob.OnStopped(ref _HitFlagElementBuffer);
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


			public void Init()
			{
				Caller = new ActionCaller(ExecFunction.Data);
			}
			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(ActionCaller.ActionCallDelegate))]
			public static void ActionCallFunction(ref ActionCaller baseCaller, int callerId, byte actionId)
			{
				ref var caller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCaller, ActionCallWrapper>(ref baseCaller);
			}
		}

	}
	static class Breakthroughenemy_89_Initializator
	{
		static System.Type jobType = typeof(Breakthroughenemy_89_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Breakthroughenemy_89_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Breakthroughenemy_89_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Breakthroughenemy_89_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Server");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Breakthroughenemy_89_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Breakthroughenemy_89_System : ISystem
	{
		EntityQuery query;
		Breakthroughenemy_89_AbilityJob job;

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
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(),
					ComponentType.ReadWrite<Unity.Transforms.LocalTransform>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.MoveAbilityComponentData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Damage>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.HitQuery>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Instigator>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(),
					ComponentType.ReadWrite<TzarGames.GameCore.HitFlagElement>(),
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
		Breakthroughenemy_89_AbilityJob createJob(ref SystemState state)
		{
			var job = new Breakthroughenemy_89_AbilityJob();
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
			job.CopyOwnerTransformToAbilityOnUpdateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.MoveAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.MoveAbilityComponentData>();
			job.CopyOwnerDamageToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(true);
			job.DamageType = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>();
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.ConvertOwnerInputToRotationAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationAbility>(true);
			job.HitQueryType = state.GetComponentTypeHandle<TzarGames.GameCore.HitQuery>();
			job.InstigatorType = state.GetComponentTypeHandle<TzarGames.GameCore.Instigator>();
			job.ModifyOwnerCharacteristicsType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>();
			job.HitFlagElementType = state.GetBufferTypeHandle<TzarGames.GameCore.HitFlagElement>();

			var ComponentLookupCharacterInputs = state.GetComponentLookup<TzarGames.GameCore.CharacterInputs>(true);
			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var ComponentLookupDamage = state.GetComponentLookup<TzarGames.GameCore.Damage>(true);
			var BufferLookupSpeedModificator = state.GetBufferLookup<TzarGames.GameCore.SpeedModificator>(true);
			var ComponentLookupDistanceMove = state.GetComponentLookup<TzarGames.GameCore.DistanceMove>(true);
			var ComponentTypeHandleDuration = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>(true);
			var BufferTypeHandleOnAbilityStartEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStartEventCommandData>(true);
			var BufferTypeHandleOnAbilityStopEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityStopEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateEventCommandData>(true);
			var BufferTypeHandleOnAbilityUpdateIdleEventCommandData = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.OnAbilityUpdateIdleEventCommandData>(true);
			var BufferTypeHandleEntityInstance = state.GetBufferTypeHandle<TzarGames.GameCore.EntityInstance>(true);



			job._ConvertOwnerInputToRotationOnStartAbilityJob.InputFromEntity = ComponentLookupCharacterInputs;
			job._ConvertOwnerInputToRotationOnStartAbilityJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity = ComponentLookupDamage;

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity = BufferLookupSpeedModificator;
			job._ModifyOwnerCharacteristicsJob.Initialize(ref state);

			job._MoveAbilityComponentJob.DistanceMoveFromEntity = ComponentLookupDistanceMove;
			job._MoveAbilityComponentJob.TransformFromEntity = ComponentLookupLocalTransform;


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
			job.CopyOwnerTransformToAbilityOnUpdateType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.MoveAbilityComponentDataType.Update(ref state);
			job.CopyOwnerDamageToAbilityType.Update(ref state);
			job.DamageType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.ConvertOwnerInputToRotationAbilityType.Update(ref state);
			job.HitQueryType.Update(ref state);
			job.InstigatorType.Update(ref state);
			job.ModifyOwnerCharacteristicsType.Update(ref state);
			job.HitFlagElementType.Update(ref state);




			job._ConvertOwnerInputToRotationOnStartAbilityJob.InputFromEntity.Update(ref state);
			job._ConvertOwnerInputToRotationOnStartAbilityJob.TransformFromEntity.Update(ref state);

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity.Update(ref state);


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity.Update(ref state);

			job._MoveAbilityComponentJob.DistanceMoveFromEntity.Update(ref state);
			job._MoveAbilityComponentJob.TransformFromEntity.Update(ref state);


			job._ScriptVizAbilityJob.DurationType.Update(ref state);
			job._ScriptVizAbilityJob.StartEventType.Update(ref state);
			job._ScriptVizAbilityJob.StopEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateEventType.Update(ref state);
			job._ScriptVizAbilityJob.UpdateIdleEventType.Update(ref state);
			job._ScriptVizAbilityJob.EntityInstanceArrayType.Update(ref state);

		}
	}
}
