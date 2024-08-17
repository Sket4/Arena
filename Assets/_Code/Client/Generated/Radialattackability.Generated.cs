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
	public struct Radialattackability_22_AbilityJob : IJobChunk
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
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown> AbilityCooldownType;
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
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AdditionalRotation> AdditionalRotationType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection> ConvertRotationToDirectionType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Direction> DirectionType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.MoveAbilityComponentData> MoveAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent> AbilityTimerEventType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData> AbilityTimerSharedDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData> AbilityTimerDataType;
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
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart> CopyOwnerTransformToAbilityOnStartType;
		public EntityTypeHandle EntityType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData> InstantiateAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData> InstantiateAbilityInstanceDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.HitQuery> HitQueryType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Instigator> InstigatorType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<Arena.Client.Abilities.AnimationAbilityComponentData> AnimationAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.Client.Animations> AnimationsType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.HitFlagElement> HitFlagElementType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<TzarGames.GameCore.Abilities.ModifyAdditionalRotation> ModifyAdditionalRotationType;

		public TzarGames.GameCore.Abilities.InstantiateAbilityComponentJob _InstantiateAbilityComponentJob;
		public TzarGames.GameCore.HitQueryAbilityComponentJob _HitQueryAbilityComponentJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdateJob _CopyOwnerTransformToAbilityOnUpdateJob;
		public TzarGames.GameCore.Abilities.MoveAbilityComponentJob _MoveAbilityComponentJob;
		public TzarGames.GameCore.Abilities.AbilityCooldownJob _AbilityCooldownJob;
		public TzarGames.GameCore.Abilities.CopyOwnerDamageToAbilityJob _CopyOwnerDamageToAbilityJob;
		public TzarGames.GameCore.Abilities.AdditionalRotationAbilityJob _AdditionalRotationAbilityJob;
		public TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbilityJob _CopyOwnerRadiusToAbilityJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStartJob _CopyOwnerTransformToAbilityOnStartJob;
		public TzarGames.GameCore.Abilities.AttackHeightJob _AttackHeightJob;
		public TzarGames.GameCore.Abilities.ConvertRotationToDirectionJob _ConvertRotationToDirectionJob;
		public Arena.Client.Abilities.AnimationAbilityComponentStartJob _AnimationAbilityComponentStartJob;
		public TzarGames.GameCore.HitFlagAbilityComponentJob _HitFlagAbilityComponentJob;
		public TzarGames.GameCore.Abilities.TimerEventAbilityComponentJob _TimerEventAbilityComponentJob;
		public TzarGames.GameCore.Abilities.ModifyAdditionalRotationJob _ModifyAdditionalRotationJob;


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
			var AbilityOwnerArray = chunk.GetNativeArray(ref AbilityOwnerType);
			var CopyOwnerTransformToAbilityOnUpdateArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnUpdateType);
			var LocalTransformArray = chunk.GetNativeArray(ref LocalTransformType);
			var AddOwnerAttackVerticalOffsetAsTranslationArray = chunk.GetNativeArray(ref AddOwnerAttackVerticalOffsetAsTranslationType);
			var AdditionalRotationArray = chunk.GetNativeArray(ref AdditionalRotationType);
			var ConvertRotationToDirectionArray = chunk.GetNativeArray(ref ConvertRotationToDirectionType);
			var DirectionArray = chunk.GetNativeArray(ref DirectionType);
			var MoveAbilityComponentDataArray = chunk.GetNativeArray(ref MoveAbilityComponentDataType);
			var AbilityTimerEventAccessor = chunk.GetBufferAccessor(ref AbilityTimerEventType);
			var AbilityTimerSharedDataArray = chunk.GetNativeArray(ref AbilityTimerSharedDataType);
			var AbilityTimerDataArray = chunk.GetNativeArray(ref AbilityTimerDataType);
			var CopyOwnerDamageToAbilityArray = chunk.GetNativeArray(ref CopyOwnerDamageToAbilityType);
			var DamageArray = chunk.GetNativeArray(ref DamageType);
			var CopyOwnerRadiusToAbilityArray = chunk.GetNativeArray(ref CopyOwnerRadiusToAbilityType);
			var RadiusArray = chunk.GetNativeArray(ref RadiusType);
			var MinimalRadiusArray = chunk.GetNativeArray(ref MinimalRadiusType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var EntityArray = chunk.GetNativeArray(EntityType);
			var InstantiateAbilityComponentDataArray = chunk.GetNativeArray(ref InstantiateAbilityComponentDataType);
			var InstantiateAbilityInstanceDataArray = chunk.GetNativeArray(ref InstantiateAbilityInstanceDataType);
			var HitQueryArray = chunk.GetNativeArray(ref HitQueryType);
			var InstigatorArray = chunk.GetNativeArray(ref InstigatorType);
			var AnimationAbilityComponentDataArray = chunk.GetNativeArray(ref AnimationAbilityComponentDataType);
			var AnimationsAccessor = chunk.GetBufferAccessor(ref AnimationsType);
			var HitFlagElementAccessor = chunk.GetBufferAccessor(ref HitFlagElementType);
			var ModifyAdditionalRotationAccessor = chunk.GetBufferAccessor(ref ModifyAdditionalRotationType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 22)
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

				if(_AbilityState.Value == AbilityStates.Idle)
				{
					_AbilityCooldownJob.OnIdleUpdate(deltaTime, ref _AbilityCooldown);
					_DurationJob.OnIdleUpdate(ref _Duration);
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
				var _CopyOwnerRadiusToAbility = CopyOwnerRadiusToAbilityArray[c];
				var _RadiusRef = new RefRW<TzarGames.GameCore.Radius>(RadiusArray, c);
				ref var _Radius = ref _RadiusRef.ValueRW;
				var _MinimalRadiusRef = new RefRW<TzarGames.GameCore.MinimalRadius>(MinimalRadiusArray, c);
				ref var _MinimalRadius = ref _MinimalRadiusRef.ValueRW;
				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _AddOwnerAttackVerticalOffsetAsTranslation = AddOwnerAttackVerticalOffsetAsTranslationArray[c];
				var _AdditionalRotationRef = new RefRW<TzarGames.GameCore.Abilities.AdditionalRotation>(AdditionalRotationArray, c);
				ref var _AdditionalRotation = ref _AdditionalRotationRef.ValueRW;
				var _AbilityTimerEventBuffer = AbilityTimerEventAccessor[c];
				var _AbilityTimerSharedData = AbilityTimerSharedDataArray[c];
				var _AbilityTimerDataRef = new RefRW<TzarGames.GameCore.Abilities.AbilityTimerData>(AbilityTimerDataArray, c);
				ref var _AbilityTimerData = ref _AbilityTimerDataRef.ValueRW;
				var _ConvertRotationToDirection = ConvertRotationToDirectionArray[c];
				var _DirectionRef = new RefRW<TzarGames.GameCore.Direction>(DirectionArray, c);
				ref var _Direction = ref _DirectionRef.ValueRW;
				var abilityEntity = EntityArray[c];
				var _InstantiateAbilityComponentData = InstantiateAbilityComponentDataArray[c];
				var _InstantiateAbilityInstanceDataRef = new RefRW<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>(InstantiateAbilityInstanceDataArray, c);
				ref var _InstantiateAbilityInstanceData = ref _InstantiateAbilityInstanceDataRef.ValueRW;
				var _HitQueryRef = new RefRW<TzarGames.GameCore.HitQuery>(HitQueryArray, c);
				ref var _HitQuery = ref _HitQueryRef.ValueRW;
				var _InstigatorRef = new RefRW<TzarGames.GameCore.Instigator>(InstigatorArray, c);
				ref var _Instigator = ref _InstigatorRef.ValueRW;
				var _MoveAbilityComponentDataRef = new RefRW<TzarGames.GameCore.Abilities.MoveAbilityComponentData>(MoveAbilityComponentDataArray, c);
				ref var _MoveAbilityComponentData = ref _MoveAbilityComponentDataRef.ValueRW;
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
				var _AnimationAbilityComponentData = AnimationAbilityComponentDataArray[c];
				var _AnimationsBuffer = AnimationsAccessor[c];

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					_DurationJob.OnStarted(ref _Duration);
					_CopyOwnerDamageToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerDamageToAbility, ref _Damage);
					_CopyOwnerRadiusToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerRadiusToAbility, ref _Radius, ref _MinimalRadius);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_AttackHeightJob.OnStarted(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_AdditionalRotationAbilityJob.OnStarted(ref _AdditionalRotation, ref _LocalTransform);
					_TimerEventAbilityComponentJob.OnStarted(ref _AbilityTimerEventBuffer, in _AbilityTimerSharedData, ref _AbilityTimerData);
					_ConvertRotationToDirectionJob.OnStarted(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_InstantiateAbilityComponentJob.OnStarted(abilityEntity, in _AbilityOwner, in _InstantiateAbilityComponentData, ref _InstantiateAbilityInstanceData, Commands, unfilteredChunkIndex, in _LocalTransform);
					_HitQueryAbilityComponentJob.OnStarted(in _AbilityOwner, ref _HitQuery, ref _Instigator);
					_MoveAbilityComponentJob.OnStarted(in _AbilityOwner, ref _MoveAbilityComponentData, _LocalTransform, unfilteredChunkIndex, Commands);
					_AbilityCooldownJob.OnStarted(ref _AbilityCooldown);
					_AnimationAbilityComponentStartJob.OnStarted(in abilityInterface, deltaTime, in _AbilityOwner, unfilteredChunkIndex, Commands, in _AnimationAbilityComponentData, _AnimationsBuffer);

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
				var _ModifyAdditionalRotationBuffer = ModifyAdditionalRotationAccessor[c];

				var callWrapper = new ActionCallWrapper();
				callWrapper.Init();
				callWrapper._ModifyAdditionalRotationJob = _ModifyAdditionalRotationJob;
				callWrapper._ModifyAdditionalRotationBuffer = _ModifyAdditionalRotationBuffer;
				callWrapper._AdditionalRotationRef = _AdditionalRotationRef;
				ref var actionCaller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCallWrapper, ActionCaller>(ref callWrapper);

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_CopyOwnerTransformToAbilityOnUpdateJob.OnUpdate(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnUpdate, ref _LocalTransform);
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
					_AttackHeightJob.OnUpdate(in _AbilityOwner, ref _LocalTransform, in _AddOwnerAttackVerticalOffsetAsTranslation);
					_AdditionalRotationAbilityJob.OnUpdate(in _AdditionalRotation, ref _LocalTransform);
					_ConvertRotationToDirectionJob.OnUpdate(in _ConvertRotationToDirection, in _LocalTransform, ref _Direction);
					_MoveAbilityComponentJob.OnUpdate(in _AbilityOwner, unfilteredChunkIndex, Commands, ref _MoveAbilityComponentData, ref _abilityControl);
					_AbilityCooldownJob.OnUpdate(deltaTime, ref _AbilityCooldown);
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
					var _HitFlagElementBuffer = HitFlagElementAccessor[c];

					_InstantiateAbilityComponentJob.OnStopped(unfilteredChunkIndex, in _InstantiateAbilityComponentData, ref _InstantiateAbilityInstanceData, Commands);
					_HitQueryAbilityComponentJob.OnStopped(ref _HitQuery);
					_DurationJob.OnStopped(ref _Duration);
					_HitFlagAbilityComponentJob.OnStopped(ref _HitFlagElementBuffer);

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

			public TzarGames.GameCore.Abilities.ModifyAdditionalRotationJob _ModifyAdditionalRotationJob;

			public DynamicBuffer<TzarGames.GameCore.Abilities.ModifyAdditionalRotation> _ModifyAdditionalRotationBuffer;
			public RefRW<TzarGames.GameCore.Abilities.AdditionalRotation> _AdditionalRotationRef;
			public void Init()
			{
				Caller = new ActionCaller(ExecFunction.Data);
			}
			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(ActionCaller.ActionCallDelegate))]
			public static void ActionCallFunction(ref ActionCaller baseCaller, int callerId, byte actionId)
			{
				ref var caller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCaller, ActionCallWrapper>(ref baseCaller);
				foreach(var _ModifyAdditionalRotation in caller._ModifyAdditionalRotationBuffer)
				{
					if(_ModifyAdditionalRotation.CallerId == callerId && _ModifyAdditionalRotation.ActionId == actionId)
					{
						caller._ModifyAdditionalRotationJob.Execute(_ModifyAdditionalRotation, ref caller._AdditionalRotationRef.ValueRW);
						break;
					}
				}
			}
		}

	}
	static class Radialattackability_22_Initializator
	{
		static System.Type jobType = typeof(Radialattackability_22_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Radialattackability_22_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Radialattackability_22_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Radialattackability_22_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Client");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Radialattackability_22_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Radialattackability_22_System : ISystem
	{
		EntityQuery query;
		Radialattackability_22_AbilityJob job;

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
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(),
					ComponentType.ReadWrite<Unity.Transforms.LocalTransform>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AdditionalRotation>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Direction>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.MoveAbilityComponentData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerEvent>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Damage>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Radius>(),
					ComponentType.ReadWrite<TzarGames.GameCore.MinimalRadius>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.HitQuery>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Instigator>(),
					ComponentType.ReadOnly<Arena.Client.Abilities.AnimationAbilityComponentData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Client.Animations>(),
					ComponentType.ReadWrite<TzarGames.GameCore.HitFlagElement>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ModifyAdditionalRotation>(),
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
		Radialattackability_22_AbilityJob createJob(ref SystemState state)
		{
			var job = new Radialattackability_22_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.NetworkPlayerLookup = SystemAPI.GetComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer>(true);
			job.PlayerControllerLookup = SystemAPI.GetComponentLookup<TzarGames.GameCore.PlayerController>(true);
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.AbilityCooldownType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown>();
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.CopyOwnerTransformToAbilityOnUpdateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.AddOwnerAttackVerticalOffsetAsTranslationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AddOwnerAttackVerticalOffsetAsTranslation>(true);
			job.AdditionalRotationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AdditionalRotation>();
			job.ConvertRotationToDirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertRotationToDirection>(true);
			job.DirectionType = state.GetComponentTypeHandle<TzarGames.GameCore.Direction>();
			job.MoveAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.MoveAbilityComponentData>();
			job.AbilityTimerEventType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent>();
			job.AbilityTimerSharedDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(true);
			job.EntityType = state.GetEntityTypeHandle();
			job.AbilityTimerDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData>();
			job.CopyOwnerDamageToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(true);
			job.DamageType = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>();
			job.CopyOwnerRadiusToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerRadiusToAbility>(true);
			job.RadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.Radius>();
			job.MinimalRadiusType = state.GetComponentTypeHandle<TzarGames.GameCore.MinimalRadius>();
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.InstantiateAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData>(true);
			job.InstantiateAbilityInstanceDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>();
			job.HitQueryType = state.GetComponentTypeHandle<TzarGames.GameCore.HitQuery>();
			job.InstigatorType = state.GetComponentTypeHandle<TzarGames.GameCore.Instigator>();
			job.AnimationAbilityComponentDataType = state.GetComponentTypeHandle<Arena.Client.Abilities.AnimationAbilityComponentData>(true);
			job.AnimationsType = state.GetBufferTypeHandle<TzarGames.GameCore.Client.Animations>(true);
			job.HitFlagElementType = state.GetBufferTypeHandle<TzarGames.GameCore.HitFlagElement>();
			job.ModifyAdditionalRotationType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.ModifyAdditionalRotation>(true);

			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var ComponentLookupDistanceMove = state.GetComponentLookup<TzarGames.GameCore.DistanceMove>(true);
			var ComponentLookupDamage = state.GetComponentLookup<TzarGames.GameCore.Damage>(true);
			var ComponentLookupAttackRadius = state.GetComponentLookup<TzarGames.GameCore.AttackRadius>(true);
			var ComponentLookupRadius = state.GetComponentLookup<TzarGames.GameCore.Radius>(true);
			var ComponentLookupAttackVerticalOffset = state.GetComponentLookup<TzarGames.GameCore.AttackVerticalOffset>(true);
			var ComponentTypeHandleAnimationAbilityStopComponentData = state.GetComponentTypeHandle<Arena.Client.Abilities.AnimationAbilityStopComponentData>();
			var ComponentTypeHandleDuration = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>(true);



			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._MoveAbilityComponentJob.DistanceMoveFromEntity = ComponentLookupDistanceMove;
			job._MoveAbilityComponentJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._CopyOwnerDamageToAbilityJob.DamageFromEntity = ComponentLookupDamage;


			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity = ComponentLookupAttackRadius;
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity = ComponentLookupRadius;


			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._AttackHeightJob.MeleeAttackerFromEntity = ComponentLookupAttackVerticalOffset;


			job._AnimationAbilityComponentStartJob.StopAnimType = ComponentTypeHandleAnimationAbilityStopComponentData;
			job._AnimationAbilityComponentStartJob.DurationType = ComponentTypeHandleDuration;




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
			job.AbilityCooldownType.Update(ref state);
			job.DurationType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnUpdateType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.AddOwnerAttackVerticalOffsetAsTranslationType.Update(ref state);
			job.AdditionalRotationType.Update(ref state);
			job.ConvertRotationToDirectionType.Update(ref state);
			job.DirectionType.Update(ref state);
			job.MoveAbilityComponentDataType.Update(ref state);
			job.AbilityTimerEventType.Update(ref state);
			job.AbilityTimerSharedDataType.Update(ref state);
			job.EntityType.Update(ref state);
			job.AbilityTimerDataType.Update(ref state);
			job.CopyOwnerDamageToAbilityType.Update(ref state);
			job.DamageType.Update(ref state);
			job.CopyOwnerRadiusToAbilityType.Update(ref state);
			job.RadiusType.Update(ref state);
			job.MinimalRadiusType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.InstantiateAbilityComponentDataType.Update(ref state);
			job.InstantiateAbilityInstanceDataType.Update(ref state);
			job.HitQueryType.Update(ref state);
			job.InstigatorType.Update(ref state);
			job.AnimationAbilityComponentDataType.Update(ref state);
			job.AnimationsType.Update(ref state);
			job.HitFlagElementType.Update(ref state);
			job.ModifyAdditionalRotationType.Update(ref state);




			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity.Update(ref state);

			job._MoveAbilityComponentJob.DistanceMoveFromEntity.Update(ref state);
			job._MoveAbilityComponentJob.TransformFromEntity.Update(ref state);


			job._CopyOwnerDamageToAbilityJob.DamageFromEntity.Update(ref state);


			job._CopyOwnerRadiusToAbilityJob.AttackRadiusFromEntity.Update(ref state);
			job._CopyOwnerRadiusToAbilityJob.RadiusFromEntity.Update(ref state);


			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);

			job._AttackHeightJob.MeleeAttackerFromEntity.Update(ref state);


			job._AnimationAbilityComponentStartJob.StopAnimType.Update(ref state);
			job._AnimationAbilityComponentStartJob.DurationType.Update(ref state);




		}
	}
}
