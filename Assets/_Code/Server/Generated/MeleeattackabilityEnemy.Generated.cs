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
	public struct MeleeattackabilityEnemy_18_AbilityJob : IJobChunk
	{
		public UniversalCommandBuffer Commands;
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
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner> AbilityOwnerType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate> CopyOwnerTransformToAbilityOnUpdateType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<Unity.Transforms.LocalTransform> LocalTransformType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation> AddOwnerAttackVerticalOffsetAsTranslationType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection> ConvertRotationToDirectionType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Direction> DirectionType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent> AbilityTimerEventType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData> AbilityTimerSharedDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData> AbilityTimerDataType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart> CopyOwnerTransformToAbilityOnStartType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility> CopyOwnerDamageToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Damage> DamageType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility> CopyOwnerRadiusToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Radius> RadiusType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.MinimalRadius> MinimalRadiusType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility> CopyOwnerCriticalToAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier> CriticalDamageMultiplierType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance> CriticalDamageChanceType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics> ModifyOwnerCharacteristicsType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<TzarGames.GameCore.Abilities.HitQueryAbilityAction> HitQueryAbilityActionType;
		public EntityTypeHandle EntityType;

		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStartJob _CopyOwnerTransformToAbilityOnStartJob;
		public TzarGames.GameCore.Abilities.ModifyOwnerCharacteristicsJob _ModifyOwnerCharacteristicsJob;
		public TzarGames.GameCore.Abilities.ConvertRotationToDirectionJob _ConvertRotationToDirectionJob;
		public TzarGames.GameCore.Abilities.CopyOwnerDamageToAbilityJob _CopyOwnerDamageToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdateJob _CopyOwnerTransformToAbilityOnUpdateJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbilityJob _CopyOwnerRadiusToAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbilityJob _CopyOwnerCriticalToAbilityJob;
		public TzarGames.GameCore.Abilities.AttackHeightJob _AttackHeightJob;
		public TzarGames.GameCore.Abilities.AbilityCylinderHitActionJob _AbilityCylinderHitActionJob;
		public TzarGames.GameCore.Abilities.TimerEventAbilityComponentJob _TimerEventAbilityComponentJob;
		public TzarGames.GameCore.Abilities.ModifyDurationByAttackSpeedJob _ModifyDurationByAttackSpeedJob;


		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			var AbilityStateArray = chunk.GetNativeArray(ref AbilityStateType);
			var AbilityIDArray = chunk.GetNativeArray(ref AbilityIDType);
			var DurationArray = chunk.GetNativeArray(ref DurationType);
			var AbilityOwnerArray = chunk.GetNativeArray(ref AbilityOwnerType);
			var CopyOwnerTransformToAbilityOnUpdateArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnUpdateType);
			var LocalTransformArray = chunk.GetNativeArray(ref LocalTransformType);
			var AddOwnerAttackVerticalOffsetAsTranslationArray = chunk.GetNativeArray(ref AddOwnerAttackVerticalOffsetAsTranslationType);
			var ConvertRotationToDirectionArray = chunk.GetNativeArray(ref ConvertRotationToDirectionType);
			var DirectionArray = chunk.GetNativeArray(ref DirectionType);
			var AbilityTimerEventAccessor = chunk.GetBufferAccessor(ref AbilityTimerEventType);
			var AbilityTimerSharedDataArray = chunk.GetNativeArray(ref AbilityTimerSharedDataType);
			var AbilityTimerDataArray = chunk.GetNativeArray(ref AbilityTimerDataType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var CopyOwnerDamageToAbilityArray = chunk.GetNativeArray(ref CopyOwnerDamageToAbilityType);
			var DamageArray = chunk.GetNativeArray(ref DamageType);
			var CopyOwnerRadiusToAbilityArray = chunk.GetNativeArray(ref CopyOwnerRadiusToAbilityType);
			var RadiusArray = chunk.GetNativeArray(ref RadiusType);
			var MinimalRadiusArray = chunk.GetNativeArray(ref MinimalRadiusType);
			var CopyOwnerCriticalToAbilityArray = chunk.GetNativeArray(ref CopyOwnerCriticalToAbilityType);
			var CriticalDamageMultiplierArray = chunk.GetNativeArray(ref CriticalDamageMultiplierType);
			var CriticalDamageChanceArray = chunk.GetNativeArray(ref CriticalDamageChanceType);
			var ModifyOwnerCharacteristicsArray = chunk.GetNativeArray(ref ModifyOwnerCharacteristicsType);
			var HitQueryAbilityActionAccessor = chunk.GetBufferAccessor(ref HitQueryAbilityActionType);
			var EntityArray = chunk.GetNativeArray(EntityType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 18)
				{
					continue;
				}
				var _AbilityState = AbilityStateArray[c];
				var _DurationRef = new RefRW<TzarGames.GameCore.Abilities.Duration>(DurationArray, c);
				ref var _Duration = ref _DurationRef.ValueRW;

				if(_AbilityState.Value == AbilityStates.Idle)
				{
					_DurationJob.OnIdleUpdate(ref _Duration);
				}

				var _AbilityOwner = AbilityOwnerArray[c];
				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _CopyOwnerDamageToAbility = CopyOwnerDamageToAbilityArray[c];
				var _DamageRef = new RefRW<TzarGames.GameCore.Damage>(DamageArray, c);
				ref var _Damage = ref _DamageRef.ValueRW;
				var _CopyOwnerRadiusToAbility = CopyOwnerRadiusToAbilityArray[c];
				var _RadiusRef = new RefRW<TzarGames.GameCore.Radius>(RadiusArray, c);
				ref var _Radius = ref _RadiusRef.ValueRW;
				var _MinimalRadiusRef = new RefRW<TzarGames.GameCore.MinimalRadius>(MinimalRadiusArray, c);
				ref var _MinimalRadius = ref _MinimalRadiusRef.ValueRW;
				var _CopyOwnerCriticalToAbility = CopyOwnerCriticalToAbilityArray[c];
				var _CriticalDamageMultiplierRef = new RefRW<TzarGames.GameCore.CriticalDamageMultiplier>(CriticalDamageMultiplierArray, c);
				ref var _CriticalDamageMultiplier = ref _CriticalDamageMultiplierRef.ValueRW;
				var _CriticalDamageChanceRef = new RefRW<TzarGames.GameCore.CriticalDamageChance>(CriticalDamageChanceArray, c);
				ref var _CriticalDamageChance = ref _CriticalDamageChanceRef.ValueRW;
				var _AddOwnerAttackVerticalOffsetAsTranslation = AddOwnerAttackVerticalOffsetAsTranslationArray[c];
				var _AbilityTimerEventBuffer = AbilityTimerEventAccessor[c];
				var _AbilityTimerSharedData = AbilityTimerSharedDataArray[c];
				var _AbilityTimerDataRef = new RefRW<TzarGames.GameCore.Abilities.AbilityTimerData>(AbilityTimerDataArray, c);
				ref var _AbilityTimerData = ref _AbilityTimerDataRef.ValueRW;
				var _ConvertRotationToDirection = ConvertRotationToDirectionArray[c];
				var _DirectionRef = new RefRW<TzarGames.GameCore.Direction>(DirectionArray, c);
				ref var _Direction = ref _DirectionRef.ValueRW;
				var _ModifyOwnerCharacteristicsRef = new RefRW<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(ModifyOwnerCharacteristicsArray, c);
				ref var _ModifyOwnerCharacteristics = ref _ModifyOwnerCharacteristicsRef.ValueRW;

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					_DurationJob.OnStarted(ref _Duration);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_CopyOwnerDamageToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerDamageToAbility, ref _Damage);
					_CopyOwnerRadiusToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerRadiusToAbility, ref _Radius, ref _MinimalRadius);
					_CopyOwnerCriticalToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerCriticalToAbility, ref _CriticalDamageMultiplier, ref _CriticalDamageChance);
					_ModifyDurationByAttackSpeedJob.OnStarted(in _AbilityOwner, ref _Duration);
					_AttackHeightJob.OnStarted(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_TimerEventAbilityComponentJob.OnStarted(ref _AbilityTimerEventBuffer, in _AbilityTimerSharedData, ref _AbilityTimerData);
					_ConvertRotationToDirectionJob.OnStarted(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_ModifyOwnerCharacteristicsJob.OnStarted(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);

					if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
					{
						isJustStarted = true;
					}
				}

				var abilityEntity = EntityArray[c];
				if (isJustStarted)
				{
					_AbilityState.Value = AbilityStates.Running;
					AbilityStateArray[c] = _AbilityState;
					var eventEntity = Commands.CreateEntity(unfilteredChunkIndex, AbilityEventArchetype);
					Commands.SetComponent(unfilteredChunkIndex, eventEntity, new AbilityEvent { AbilityEntity = abilityEntity, EventType = AbilityEvents.Started });
				}

				var _CopyOwnerTransformToAbilityOnUpdate = CopyOwnerTransformToAbilityOnUpdateArray[c];
				float deltaTime;
				if(DeltaTimeFromEntity.HasComponent(_AbilityOwner.Value))
				{
					deltaTime = DeltaTimeFromEntity[_AbilityOwner.Value].Value;
				}
				else
				{
					deltaTime = GlobalDeltaTime;
				}
				var _HitQueryAbilityActionBuffer = HitQueryAbilityActionAccessor[c];
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

				var callWrapper = new ActionCallWrapper();
				callWrapper.Init();
				callWrapper._AbilityCylinderHitActionJob = _AbilityCylinderHitActionJob;
				callWrapper.unfilteredChunkIndex = unfilteredChunkIndex;
				callWrapper._AbilityOwner = _AbilityOwner;
				callWrapper._HitQueryAbilityActionBuffer = _HitQueryAbilityActionBuffer;
				callWrapper._LocalTransformRef = _LocalTransformRef;
				callWrapper.abilityInterface = abilityInterface;
				callWrapper.Commands = Commands;
				ref var actionCaller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCallWrapper, ActionCaller>(ref callWrapper);

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_CopyOwnerTransformToAbilityOnUpdateJob.OnUpdate(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnUpdate, ref _LocalTransform);
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
					_AttackHeightJob.OnUpdate(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_ConvertRotationToDirectionJob.OnUpdate(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_TimerEventAbilityComponentJob.OnUpdate(ref _AbilityTimerEventBuffer, ref actionCaller, in _Duration, in _AbilityTimerSharedData, in abilityInterface, ref _AbilityTimerData);

					if(_abilityControl.StopRequest)
					{
						_AbilityState.Value = AbilityStates.Stopped;
					}

				}
				if(_AbilityState.Value == AbilityStates.Stopped)
				{
					var eventEntity = Commands.CreateEntity(unfilteredChunkIndex, AbilityEventArchetype);
					Commands.SetComponent(unfilteredChunkIndex, eventEntity, new AbilityEvent { AbilityEntity = abilityEntity, EventType = AbilityEvents.Stopped });
					_ModifyOwnerCharacteristicsJob.OnStopped(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_DurationJob.OnStopped(ref _Duration);

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

			public int unfilteredChunkIndex;
			public TzarGames.GameCore.Abilities.AbilityOwner _AbilityOwner;
			public DynamicBuffer<TzarGames.GameCore.Abilities.HitQueryAbilityAction> _HitQueryAbilityActionBuffer;
			public RefRW<Unity.Transforms.LocalTransform> _LocalTransformRef;
			public AbilityInterface abilityInterface;
			public UniversalCommandBuffer Commands;
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
			}
		}

	}
	static class MeleeattackabilityEnemy_18_Initializator
	{
		static System.Type jobType = typeof(MeleeattackabilityEnemy_18_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(MeleeattackabilityEnemy_18_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				MeleeattackabilityEnemy_18_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(MeleeattackabilityEnemy_18_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Server");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<MeleeattackabilityEnemy_18_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct MeleeattackabilityEnemy_18_System : ISystem
	{
		EntityQuery query;
		MeleeattackabilityEnemy_18_AbilityJob job;

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
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(),
					ComponentType.ReadWrite<Unity.Transforms.LocalTransform>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Direction>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerEvent>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Damage>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Radius>(),
					ComponentType.ReadWrite<TzarGames.GameCore.MinimalRadius>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.CriticalDamageMultiplier>(),
					ComponentType.ReadWrite<TzarGames.GameCore.CriticalDamageChance>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.HitQueryAbilityAction>(),
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
		MeleeattackabilityEnemy_18_AbilityJob createJob(ref SystemState state)
		{
			var job = new MeleeattackabilityEnemy_18_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.NetworkPlayerLookup = SystemAPI.GetComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer>(true);
			job.PlayerControllerLookup = SystemAPI.GetComponentLookup<TzarGames.GameCore.PlayerController>(true);
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.CopyOwnerTransformToAbilityOnUpdateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.AddOwnerAttackVerticalOffsetAsTranslationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(true);
			job.ConvertRotationToDirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(true);
			job.DirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Direction>();
			job.AbilityTimerEventType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent>();
			job.AbilityTimerSharedDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(true);
			job.EntityType = state.GetEntityTypeHandle();
			job.AbilityTimerDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData>();
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.CopyOwnerDamageToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(true);
			job.DamageType = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>();
			job.CopyOwnerRadiusToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(true);
			job.RadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.Radius>();
			job.MinimalRadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.MinimalRadius>();
			job.CopyOwnerCriticalToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerCriticalToAbility>(true);
			job.CriticalDamageMultiplierType = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier>();
			job.CriticalDamageChanceType = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance>();
			job.ModifyOwnerCharacteristicsType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>();
			job.HitQueryAbilityActionType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.HitQueryAbilityAction>(true);

			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var BufferLookupSpeedModificator = state.GetBufferLookup<TzarGames.GameCore.SpeedModificator>(true);
			var ComponentLookupDamage = state.GetComponentLookup<TzarGames.GameCore.Damage>(true);
			var ComponentLookupAttackRadius = state.GetComponentLookup<TzarGames.GameCore.AttackRadius>(true);
			var ComponentLookupRadius = state.GetComponentLookup<TzarGames.GameCore.Radius>(true);
			var ComponentLookupCriticalDamageMultiplier = state.GetComponentLookup<TzarGames.GameCore.CriticalDamageMultiplier>(true);
			var ComponentLookupCriticalDamageChance = state.GetComponentLookup<TzarGames.GameCore.CriticalDamageChance>(true);
			var ComponentLookupAttackVerticalOffset = state.GetComponentLookup<TzarGames.GameCore.AttackVerticalOffset>(true);
			var ComponentTypeHandleAngle = state.GetComponentTypeHandle<TzarGames.GameCore.Angle>(true);
			var ComponentTypeHandleLocalTransform = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>(true);
			var ComponentTypeHandleRadius = state.GetComponentTypeHandle<TzarGames.GameCore.Radius>(true);
			var ComponentTypeHandleMinimalRadius = state.GetComponentTypeHandle<TzarGames.GameCore.MinimalRadius>(true);
			var ComponentTypeHandleDamage = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>(true);
			var ComponentTypeHandleHeight = state.GetComponentTypeHandle<TzarGames.GameCore.Height>(true);
			var ComponentTypeHandleInstigator = state.GetComponentTypeHandle<TzarGames.GameCore.Instigator>(true);
			var ComponentTypeHandleCriticalDamageChance = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageChance>(true);
			var ComponentTypeHandleCriticalDamageMultiplier = state.GetComponentTypeHandle<TzarGames.GameCore.CriticalDamageMultiplier>(true);
			var BufferTypeHandleEntityInstance = state.GetBufferTypeHandle<TzarGames.GameCore.EntityInstance>();
			var ComponentLookupTarget = state.GetComponentLookup<TzarGames.GameCore.Target>(true);
			var ComponentTypeHandleWeaponSurface = state.GetComponentTypeHandle<TzarGames.GameCore.WeaponSurface>(true);
			var ComponentLookupAttackSpeed = state.GetComponentLookup<TzarGames.GameCore.AttackSpeed>(true);

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity = BufferLookupSpeedModificator;
			job._ModifyOwnerCharacteristicsJob.Initialize(ref state);


			job._CopyOwnerDamageToAbilityJob.DamageFromEntity = ComponentLookupDamage;

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity = ComponentLookupAttackRadius;
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity = ComponentLookupRadius;

			job._CopyOwnerCriticalToAbilityJob.MultiplierFromEntity = ComponentLookupCriticalDamageMultiplier;
			job._CopyOwnerCriticalToAbilityJob.CritChanceFromEntity = ComponentLookupCriticalDamageChance;

			job._AttackHeightJob.MeleeAttackerFromEntity = ComponentLookupAttackVerticalOffset;

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


			job._ModifyDurationByAttackSpeedJob.AttackSpeedFromEntity = ComponentLookupAttackSpeed;

			return job;
		}
		void updateJob(ref SystemState state)
		{
			var cmdSingleton = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
			job.Commands = new UniversalCommandBuffer(cmdSingleton.CreateCommandBuffer(state.WorldUnmanaged));
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.IsServer = abilitySystem.IsServer;
			job.NetworkPlayerLookup.Update(ref state);
			job.PlayerControllerLookup.Update(ref state);
			job.GlobalDeltaTime = SystemAPI.Time.DeltaTime;
			job.DeltaTimeFromEntity.Update(ref state);
			job.AbilityStateType.Update(ref state);
			job.AbilityIDType.Update(ref state);
			job.DurationType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnUpdateType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.AddOwnerAttackVerticalOffsetAsTranslationType.Update(ref state);
			job.ConvertRotationToDirectionType.Update(ref state);
			job.DirectionType.Update(ref state);
			job.AbilityTimerEventType.Update(ref state);
			job.AbilityTimerSharedDataType.Update(ref state);
			job.EntityType.Update(ref state);
			job.AbilityTimerDataType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.CopyOwnerDamageToAbilityType.Update(ref state);
			job.DamageType.Update(ref state);
			job.CopyOwnerRadiusToAbilityType.Update(ref state);
			job.RadiusType.Update(ref state);
			job.MinimalRadiusType.Update(ref state);
			job.CopyOwnerCriticalToAbilityType.Update(ref state);
			job.CriticalDamageMultiplierType.Update(ref state);
			job.CriticalDamageChanceType.Update(ref state);
			job.ModifyOwnerCharacteristicsType.Update(ref state);
			job.HitQueryAbilityActionType.Update(ref state);


			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);

			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity.Update(ref state);


			job._CopyOwnerDamageToAbilityJob.DamageFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity.Update(ref state);


			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity.Update(ref state);
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity.Update(ref state);

			job._CopyOwnerCriticalToAbilityJob.MultiplierFromEntity.Update(ref state);
			job._CopyOwnerCriticalToAbilityJob.CritChanceFromEntity.Update(ref state);

			job._AttackHeightJob.MeleeAttackerFromEntity.Update(ref state);

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


			job._ModifyDurationByAttackSpeedJob.AttackSpeedFromEntity.Update(ref state);

		}
	}
}
