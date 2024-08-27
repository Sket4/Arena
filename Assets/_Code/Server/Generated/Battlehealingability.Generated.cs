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
	public struct Battlehealingability_23_AbilityJob : IJobChunk
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
		public BufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent> AbilityTimerEventType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData> AbilityTimerSharedDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData> AbilityTimerDataType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner> AbilityOwnerType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart> CopyOwnerTransformToAbilityOnStartType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<Unity.Transforms.LocalTransform> LocalTransformType;
		public EntityTypeHandle EntityType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData> InstantiateAbilityComponentDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData> InstantiateAbilityInstanceDataType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData> SetOwnerAsTargetAbilityDataType;
		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<TzarGames.GameCore.Target> TargetType;
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly] public BufferTypeHandle<TzarGames.GameCore.Abilities.HealthActionData> HealthActionDataType;

		public TzarGames.GameCore.Abilities.InstantiateAbilityComponentJob _InstantiateAbilityComponentJob;
		public TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStartJob _CopyOwnerTransformToAbilityOnStartJob;
		public TzarGames.GameCore.Abilities.AbilityCooldownJob _AbilityCooldownJob;
		public TzarGames.GameCore.Abilities.SetTargetAbilityJob _SetTargetAbilityJob;
		public TzarGames.GameCore.Abilities.DurationJob _DurationJob;
		public TzarGames.GameCore.Abilities.TimerEventAbilityComponentJob _TimerEventAbilityComponentJob;
		public TzarGames.GameCore.Abilities.HealthAbilityActionJob _HealthAbilityActionJob;


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
			var AbilityTimerEventAccessor = chunk.GetBufferAccessor(ref AbilityTimerEventType);
			var AbilityTimerSharedDataArray = chunk.GetNativeArray(ref AbilityTimerSharedDataType);
			var AbilityTimerDataArray = chunk.GetNativeArray(ref AbilityTimerDataType);
			var AbilityOwnerArray = chunk.GetNativeArray(ref AbilityOwnerType);
			var CopyOwnerTransformToAbilityOnStartArray = chunk.GetNativeArray(ref CopyOwnerTransformToAbilityOnStartType);
			var LocalTransformArray = chunk.GetNativeArray(ref LocalTransformType);
			var EntityArray = chunk.GetNativeArray(EntityType);
			var InstantiateAbilityComponentDataArray = chunk.GetNativeArray(ref InstantiateAbilityComponentDataType);
			var InstantiateAbilityInstanceDataArray = chunk.GetNativeArray(ref InstantiateAbilityInstanceDataType);
			var SetOwnerAsTargetAbilityDataArray = chunk.GetNativeArray(ref SetOwnerAsTargetAbilityDataType);
			var TargetArray = chunk.GetNativeArray(ref TargetType);
			var HealthActionDataAccessor = chunk.GetBufferAccessor(ref HealthActionDataType);

			var entityCount = chunk.Count;

			for (int c=0; c < entityCount; c++)
			{
				var _AbilityID = AbilityIDArray[c];
				if(_AbilityID.Value != 23)
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

				var _CopyOwnerTransformToAbilityOnStart = CopyOwnerTransformToAbilityOnStartArray[c];
				var _LocalTransformRef = new RefRW<Unity.Transforms.LocalTransform>(LocalTransformArray, c);
				ref var _LocalTransform = ref _LocalTransformRef.ValueRW;
				var _AbilityTimerEventBuffer = AbilityTimerEventAccessor[c];
				var _AbilityTimerSharedData = AbilityTimerSharedDataArray[c];
				var _AbilityTimerDataRef = new RefRW<TzarGames.GameCore.Abilities.AbilityTimerData>(AbilityTimerDataArray, c);
				ref var _AbilityTimerData = ref _AbilityTimerDataRef.ValueRW;
				var abilityEntity = EntityArray[c];
				var _InstantiateAbilityComponentData = InstantiateAbilityComponentDataArray[c];
				var _InstantiateAbilityInstanceDataRef = new RefRW<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>(InstantiateAbilityInstanceDataArray, c);
				ref var _InstantiateAbilityInstanceData = ref _InstantiateAbilityInstanceDataRef.ValueRW;
				var _SetOwnerAsTargetAbilityData = SetOwnerAsTargetAbilityDataArray[c];
				var _TargetRef = new RefRW<TzarGames.GameCore.Target>(TargetArray, c);
				ref var _Target = ref _TargetRef.ValueRW;

				bool isJustStarted = false;

				if (_AbilityState.Value == AbilityStates.WaitingForValidation || _AbilityState.Value == AbilityStates.ValidatedAndWaitingForStart)
				{
					_DurationJob.OnStarted(ref _Duration);
					_CopyOwnerTransformToAbilityOnStartJob.OnStarted(in _AbilityOwner, in _CopyOwnerTransformToAbilityOnStart, ref _LocalTransform);
					_TimerEventAbilityComponentJob.OnStarted(ref _AbilityTimerEventBuffer, in _AbilityTimerSharedData, ref _AbilityTimerData);
					_InstantiateAbilityComponentJob.OnStarted(abilityEntity, in _AbilityOwner, in _InstantiateAbilityComponentData, ref _InstantiateAbilityInstanceData, Commands, unfilteredChunkIndex, in _LocalTransform);
					_AbilityCooldownJob.OnStarted(ref _AbilityCooldown);
					_SetTargetAbilityJob.OnStarted(in _AbilityOwner, in _SetOwnerAsTargetAbilityData, ref _Target);

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

				var _HealthActionDataBuffer = HealthActionDataAccessor[c];

				var callWrapper = new ActionCallWrapper();
				callWrapper.Init();
				callWrapper._HealthAbilityActionJob = _HealthAbilityActionJob;
				callWrapper._HealthActionDataBuffer = _HealthActionDataBuffer;
				callWrapper._TargetRef = _TargetRef;
				callWrapper.Commands = Commands;
				callWrapper.unfilteredChunkIndex = unfilteredChunkIndex;
				ref var actionCaller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCallWrapper, ActionCaller>(ref callWrapper);
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

				if(_AbilityState.Value == AbilityStates.Running)
				{
					AbilityControl _abilityControl = default;
					_DurationJob.OnUpdate(deltaTime, ref _Duration, ref _abilityControl);
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
					_InstantiateAbilityComponentJob.OnStopped(unfilteredChunkIndex, in _InstantiateAbilityComponentData, ref _InstantiateAbilityInstanceData, Commands);
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

			public TzarGames.GameCore.Abilities.HealthAbilityActionJob _HealthAbilityActionJob;

			public DynamicBuffer<TzarGames.GameCore.Abilities.HealthActionData> _HealthActionDataBuffer;
			public RefRW<TzarGames.GameCore.Target> _TargetRef;
			public UniversalCommandBuffer Commands;
			public int unfilteredChunkIndex;
			public void Init()
			{
				Caller = new ActionCaller(ExecFunction.Data);
			}
			[BurstCompile]
			[AOT.MonoPInvokeCallback(typeof(ActionCaller.ActionCallDelegate))]
			public static void ActionCallFunction(ref ActionCaller baseCaller, int callerId, byte actionId)
			{
				ref var caller = ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<ActionCaller, ActionCallWrapper>(ref baseCaller);
				foreach(var _HealthActionData in caller._HealthActionDataBuffer)
				{
					if(_HealthActionData.CallerId == callerId && _HealthActionData.ActionId == actionId)
					{
						caller._HealthAbilityActionJob.Execute(_HealthActionData, in caller._TargetRef.ValueRW, caller.Commands, caller.unfilteredChunkIndex);
						break;
					}
				}
			}
		}

	}
	static class Battlehealingability_23_Initializator
	{
		static System.Type jobType = typeof(Battlehealingability_23_AbilityJob);

		[UnityEngine.RuntimeInitializeOnLoadMethod]
		static void initialize()
		{
			if(Battlehealingability_23_AbilityJob.ActionCallWrapper.ExecFunction.Data.IsCreated == false)
			{
				Battlehealingability_23_AbilityJob.ActionCallWrapper.ExecFunction.Data = BurstCompiler.CompileFunctionPointer<ActionCaller.ActionCallDelegate>(Battlehealingability_23_AbilityJob.ActionCallWrapper.ActionCallFunction);
			}
			AbilitySystem.RegisterAbilitySystem(jobType, createSystemCallback, "Server");
		}

		static SystemHandle createSystemCallback(World world)
		{
			return world.CreateSystem<Battlehealingability_23_System>();
		}

	}
	[BurstCompile]
	[DisableAutoCreation]
	partial struct Battlehealingability_23_System : ISystem
	{
		EntityQuery query;
		Battlehealingability_23_AbilityJob job;

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
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerEvent>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.AbilityTimerData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityOwner>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(),
					ComponentType.ReadWrite<Unity.Transforms.LocalTransform>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData>(),
					ComponentType.ReadWrite<TzarGames.GameCore.Target>(),
					ComponentType.ReadOnly<TzarGames.GameCore.Abilities.HealthActionData>(),
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
		Battlehealingability_23_AbilityJob createJob(ref SystemState state)
		{
			var job = new Battlehealingability_23_AbilityJob();
			var abilitySystem = SystemAPI.GetSingleton<AbilitySystem.Singleton>();
			job.AbilityEventArchetype = abilitySystem.AbilityEventArchetype;
			job.NetworkPlayerLookup = SystemAPI.GetComponentLookup<TzarGames.MultiplayerKit.NetworkPlayer>(true);
			job.PlayerControllerLookup = SystemAPI.GetComponentLookup<TzarGames.GameCore.PlayerController>(true);
			job.DeltaTimeFromEntity = state.GetComponentLookup<DeltaTime>(true);
			job.AbilityStateType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityState>();
			job.AbilityIDType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true);
			job.AbilityCooldownType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityCooldown>();
			job.DurationType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.Duration>();
			job.AbilityTimerEventType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerEvent>();
			job.AbilityTimerSharedDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerSharedData>(true);
			job.EntityType = state.GetEntityTypeHandle();
			job.AbilityTimerDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityTimerData>();
			job.AbilityOwnerType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true);
			job.CopyOwnerTransformToAbilityOnStartType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.CopyOwnerTransformToAbilityOnStart>(true);
			job.LocalTransformType = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>();
			job.InstantiateAbilityComponentDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityComponentData>(true);
			job.InstantiateAbilityInstanceDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.InstantiateAbilityInstanceData>();
			job.SetOwnerAsTargetAbilityDataType = state.GetComponentTypeHandle<TzarGames.GameCore.Abilities.SetOwnerAsTargetAbilityData>(true);
			job.TargetType = state.GetComponentTypeHandle<TzarGames.GameCore.Target>();
			job.HealthActionDataType = state.GetBufferTypeHandle<TzarGames.GameCore.Abilities.HealthActionData>(true);

			var ComponentLookupLocalTransform = state.GetComponentLookup<Unity.Transforms.LocalTransform>(true);
			var ComponentLookupTarget = state.GetComponentLookup<TzarGames.GameCore.Target>(true);


			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity = ComponentLookupLocalTransform;


			job._SetTargetAbilityJob.TargetFromEntity = ComponentLookupTarget;




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
			job.AbilityTimerEventType.Update(ref state);
			job.AbilityTimerSharedDataType.Update(ref state);
			job.EntityType.Update(ref state);
			job.AbilityTimerDataType.Update(ref state);
			job.AbilityOwnerType.Update(ref state);
			job.CopyOwnerTransformToAbilityOnStartType.Update(ref state);
			job.LocalTransformType.Update(ref state);
			job.InstantiateAbilityComponentDataType.Update(ref state);
			job.InstantiateAbilityInstanceDataType.Update(ref state);
			job.SetOwnerAsTargetAbilityDataType.Update(ref state);
			job.TargetType.Update(ref state);
			job.HealthActionDataType.Update(ref state);

			var singleton_TzarGamesGameCoreModifyHealthSystemSingleton = SystemAPI.GetSingleton<TzarGames.GameCore.ModifyHealthSystem.Singleton>();


			job._CopyOwnerTransformToAbilityOnStartJob.TransformFromEntity.Update(ref state);


			job._SetTargetAbilityJob.TargetFromEntity.Update(ref state);



			job._HealthAbilityActionJob.ModifyHealthSystemSingleton = singleton_TzarGamesGameCoreModifyHealthSystemSingleton;

		}
	}
}
