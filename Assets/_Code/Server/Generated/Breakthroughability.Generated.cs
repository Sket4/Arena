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
	public struct Breakthroughability_21_AbilityJob : IJobChunk
	{
		public EntityCommandBuffer.ParallelWriter Commands;
		public EntityArchetype AbilityEventArchetype;
		public bool IsServer;
		public float GlobalDeltaTime;
		[ReadOnly] public ComponentLookup<DeltaTime> DeltaTimeFromEntity;
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
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ChangeOwnerColliderAbility> ChangeOwnerColliderAbilityType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.HitQuery> HitQueryType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Instigator> InstigatorType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics> ModifyOwnerCharacteristicsType;
		[NativeDisableContainerSafetyRestriction]
		public BufferTypeHandle<TzarGames.GameCore.HitFlagElement> HitFlagElementType;
		public EntityTypeHandle EntityType;

		public TzarGames.GameCore.Abilities.ChangeOwnerColliderAbilityJob _ChangeOwnerColliderAbilityJob;
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
			var MoveAbilityComponentDataArray = chunk.GetNativeArray(ref MoveAbilityComponentDataType);
			var CopyOwnerDamageToAbilityArray = chunk.GetNativeArray(ref CopyOwnerDamageToAbilityType);
			var DamageArray = chunk.GetNativeArray(ref DamageType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var ConvertOwnerInputToRotationAbilityArray = chunk.GetNativeArray(ref ConvertOwnerInputToRotationAbilityType);
			var ChangeOwnerColliderAbilityArray = chunk.GetNativeArray(ref ChangeOwnerColliderAbilityType);
			var HitQueryArray = chunk.GetNativeArray(ref HitQueryType);
			var InstigatorArray = chunk.GetNativeArray(ref InstigatorType);
			var ModifyOwnerCharacteristicsArray = chunk.GetNativeArray(ref ModifyOwnerCharacteristicsType);
			var HitFlagElementAccessor = chunk.GetBufferAccessor(ref HitFlagElementType);
			var EntityArray = chunk.GetNativeArray(EntityType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 21)
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

				if(_AbilityState.Value == AbilityStates.Idle || _AbilityState.Value == AbilityStates.WaitingForValidation)
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
				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _ConvertOwnerInputToRotationAbility = ConvertOwnerInputToRotationAbilityArray[c];
				var _ChangeOwnerColliderAbilityRef = new RefRW<TzarGames.GameCore.Abilities.ChangeOwnerColliderAbility>(ChangeOwnerColliderAbilityArray, c);
				ref var _ChangeOwnerColliderAbility = ref _ChangeOwnerColliderAbilityRef.ValueRW;
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
					_DurationJob.OnStarted(ref _Duration);
					_CopyOwnerDamageToAbilityJob.OnStarted(in _AbilityOwner, in _CopyOwnerDamageToAbility, ref _Damage);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_ConvertOwnerInputToRotationOnStartAbilityJob.OnStarted(in _AbilityOwner, in _ConvertOwnerInputToRotationAbility, ref _LocalTransform);
					_ChangeOwnerColliderAbilityJob.OnStarted(in _AbilityOwner, ref _ChangeOwnerColliderAbility, unfilteredChunkIndex, Commands);
					_HitQueryAbilityComponentJob.OnStarted(in _AbilityOwner, ref _HitQuery, ref _Instigator);
					_AbilityCooldownJob.OnStarted(ref _AbilityCooldown);
					_ModifyOwnerCharacteristicsJob.OnStarted(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
					_MoveAbilityComponentJob.OnStarted(in _AbilityOwner, ref _MoveAbilityComponentData, _LocalTransform, unfilteredChunkIndex, Commands);

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

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_CopyOwnerTransformToAbilityOnUpdateJob.OnUpdate(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnUpdate, ref _LocalTransform);
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
					_AbilityCooldownJob.OnUpdate(deltaTime, ref _AbilityCooldown);
					_MoveAbilityComponentJob.OnUpdate(in _AbilityOwner, unfilteredChunkIndex, Commands, ref _MoveAbilityComponentData, ref _abilityControl);

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

					_ChangeOwnerColliderAbilityJob.OnStopped(in _AbilityOwner, ref _ChangeOwnerColliderAbility, unfilteredChunkIndex, Commands);
					_HitQueryAbilityComponentJob.OnStopped(ref _HitQuery);
					_DurationJob.OnStopped(ref _Duration);
					_ModifyOwnerCharacteristicsJob.OnStopped(in _AbilityOwner, unfilteredChunkIndex, ref _ModifyOwnerCharacteristics, Commands);
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
	static class Breakthroughability_21_Initializator
	{
		static System.Type jobType = typeof(Breakthroughability_21_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Breakthroughability_21_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Breakthroughability_21_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Breakthroughability_21_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Server");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Breakthroughability_21_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Breakthroughability_21_System : ISystem
	{
		EntityQuery query;
		Breakthroughability_21_AbilityJob job;

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
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.MoveAbilityComponentData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Damage>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationAbility>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.ChangeOwnerColliderAbility>(),
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
		Breakthroughability_21_AbilityJob createJob(ref SystemState state)
		{
			var job = new Breakthroughability_21_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.AbilityCooldownType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown>();
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.CopyOwnerTransformToAbilityOnUpdateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnUpdate>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.MoveAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.MoveAbilityComponentData>();
			job.CopyOwnerDamageToAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerDamageToAbility>(true);
			job.DamageType = state.GetComponentTypeHandle<TzarGames.GameCore.Damage>();
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.ConvertOwnerInputToRotationAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ConvertOwnerInputToRotationAbility>(true);
			job.ChangeOwnerColliderAbilityType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ChangeOwnerColliderAbility>();
			job.HitQueryType = state.GetComponentTypeHandle<TzarGames.GameCore.HitQuery>();
			job.InstigatorType = state.GetComponentTypeHandle<TzarGames.GameCore.Instigator>();
			job.ModifyOwnerCharacteristicsType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.ModifyOwnerCharacteristics>();
			job.HitFlagElementType = state.GetBufferTypeHandle<TzarGames.GameCore.HitFlagElement>();
			job.EntityType = state.GetEntityTypeHandle();

			var ComponentLookupPhysicsCollider = state.GetComponentLookup<Unity.Physics.PhysicsCollider>(true);
			var ComponentLookupCharacterInputs = state.GetComponentLookup<TzarGames.GameCore.CharacterInputs>(true);
			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var ComponentLookupDamage = state.GetComponentLookup<TzarGames.GameCore.Damage>(true);
			var BufferLookupSpeedModificator = state.GetBufferLookup<TzarGames.GameCore.SpeedModificator>(true);
			var ComponentLookupDistanceMove = state.GetComponentLookup<TzarGames.GameCore.DistanceMove>(true);

			job._ChangeOwnerColliderAbilityJob.ColliderFromEntity = ComponentLookupPhysicsCollider;



			job._ConvertOwnerInputToRotationOnStartAbilityJob.InputFromEntity = ComponentLookupCharacterInputs;
			job._ConvertOwnerInputToRotationOnStartAbilityJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity = ComponentLookupDamage;

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity = BufferLookupSpeedModificator;
			job._ModifyOwnerCharacteristicsJob.Initialize(ref state);

			job._MoveAbilityComponentJob.DistanceMoveFromEntity = ComponentLookupDistanceMove;
			job._MoveAbilityComponentJob.TransformFromEntity = ComponentLookupLocalTransform;


			return job;
		}
		void updateJob(ref SystemState state)
		{
			var cmdSingleton = SystemAPI.GetSingleton<GameCommandBufferSystem.Singleton>();
			job.Commands = cmdSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.IsServer = abilitySystem.IsServer;
			job.GlobalDeltaTime = SystemAPI.Time.DeltaTime;
			job.DeltaTimeFromEntity.Update(ref state);
			job.AbilityStateType.Update(ref state);
			job.AbilityIDType.Update(ref state);
			job.AbilityCooldownType.Update(ref state);
			job.DurationType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnUpdateType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.MoveAbilityComponentDataType.Update(ref state);
			job.CopyOwnerDamageToAbilityType.Update(ref state);
			job.DamageType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.ConvertOwnerInputToRotationAbilityType.Update(ref state);
			job.ChangeOwnerColliderAbilityType.Update(ref state);
			job.HitQueryType.Update(ref state);
			job.InstigatorType.Update(ref state);
			job.ModifyOwnerCharacteristicsType.Update(ref state);
			job.HitFlagElementType.Update(ref state);
			job.EntityType.Update(ref state);


			job._ChangeOwnerColliderAbilityJob.ColliderFromEntity.Update(ref state);



			job._ConvertOwnerInputToRotationOnStartAbilityJob.InputFromEntity.Update(ref state);
			job._ConvertOwnerInputToRotationOnStartAbilityJob.TransformFromEntity.Update(ref state);

			job._CopyOwnerDamageToAbilityJob.DamageFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);

			job._CopyOwnerTransformToAbilityOnUpdateJob.TransformFromEntity.Update(ref state);


			job._ModifyOwnerCharacteristicsJob.SpeedModificatorFromEntity.Update(ref state);

			job._MoveAbilityComponentJob.DistanceMoveFromEntity.Update(ref state);
			job._MoveAbilityComponentJob.TransformFromEntity.Update(ref state);


		}
	}
}
