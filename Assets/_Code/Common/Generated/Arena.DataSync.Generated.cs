using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;

[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_DataProcessor,TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync.Data,TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_DataProcessor,TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync.Data,TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_DataProcessor,TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync.Data,TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_DataProcessor,TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync.Data,TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.DifficultyData_DataProcessor,TzarGames.MultiplayerKit.Generated.DifficultyData_Sync.Data,TzarGames.MultiplayerKit.Generated.DifficultyData_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.DifficultyData_DataProcessor,TzarGames.MultiplayerKit.Generated.DifficultyData_Sync.Data,TzarGames.MultiplayerKit.Generated.DifficultyData_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.DynamicBufferDataSync<TzarGames.MultiplayerKit.Generated.SceneSectionState_DataProcessor,TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync.Data,TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.DynamicBufferDataSync<TzarGames.MultiplayerKit.Generated.SceneSectionState_DataProcessor,TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync.Data,TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.CharacterClassData_DataProcessor,TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync.Data,TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.CharacterClassData_DataProcessor,TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync.Data,TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync.Tag>.SerializeJob))]

namespace TzarGames.MultiplayerKit.Generated
{
	public struct ArenaMatchStateData_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<Arena.ArenaMatchStateData> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<ArenaMatchStateData_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			Arena.ArenaMatchStateData sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new Arena.ArenaMatchStateData();
			}
			sourceData.State = data.State;
			sourceData.Saved = data.Saved;
			sourceData.CurrentStage = data.CurrentStage;
			sourceData.IsMatchComplete = data.IsMatchComplete;
			sourceData.MatchEndTime = data.MatchEndTime;
			sourceData.DecisionWaitTime = data.DecisionWaitTime;
			sourceData.IsNextSceneAvailable = data.IsNextSceneAvailable;
			sourceData.IsMatchFailed = data.IsMatchFailed;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class ArenaMatchStateData_Sync : ComponentDataSync<ArenaMatchStateData_DataProcessor, ArenaMatchStateData_Sync.Data, ArenaMatchStateData_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(ArenaMatchStateData_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<Arena.ArenaMatchStateData>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public Arena.ArenaMatchState State;
			public System.Boolean Saved;
			public System.UInt16 CurrentStage;
			public System.Boolean IsMatchComplete;
			public System.Double MatchEndTime;
			public System.Single DecisionWaitTime;
			public System.Boolean IsNextSceneAvailable;
			public System.Boolean IsMatchFailed;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<Arena.ArenaMatchStateData>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<Arena.ArenaMatchStateData> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<Arena.ArenaMatchStateData> sources = default;
				if(IsZeroSized == false)
				{
					sources = chunk.GetNativeArray(SourceType);
				}
				for(int i=0; i<chunk.Count; i++)
				{
					var networkId = netIds[i];
					if(networkId.IsValid == false)
					{
						continue;
					}
					var data = new Data();
					if(IsZeroSized == false)
					{
						var source = sources[i];
						data.State = source.State;
						data.Saved = source.Saved;
						data.CurrentStage = source.CurrentStage;
						data.IsMatchComplete = source.IsMatchComplete;
						data.MatchEndTime = source.MatchEndTime;
						data.DecisionWaitTime = source.DecisionWaitTime;
						data.IsNextSceneAvailable = source.IsNextSceneAvailable;
						data.IsMatchFailed = source.IsMatchFailed;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out ArenaMatchStateData_DataProcessor dataProcessor)
		{
			dataProcessor = new ArenaMatchStateData_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<Arena.ArenaMatchStateData>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct SafeZoneSyncData_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<Arena.Server.SafeZoneSyncData> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<SafeZoneSyncData_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			Arena.Server.SafeZoneSyncData sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new Arena.Server.SafeZoneSyncData();
			}
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class SafeZoneSyncData_Sync : ComponentDataSync<SafeZoneSyncData_DataProcessor, SafeZoneSyncData_Sync.Data, SafeZoneSyncData_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(SafeZoneSyncData_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<Arena.Server.SafeZoneSyncData>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<Arena.Server.SafeZoneSyncData>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<Arena.Server.SafeZoneSyncData> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<Arena.Server.SafeZoneSyncData> sources = default;
				if(IsZeroSized == false)
				{
					sources = chunk.GetNativeArray(SourceType);
				}
				for(int i=0; i<chunk.Count; i++)
				{
					var networkId = netIds[i];
					if(networkId.IsValid == false)
					{
						continue;
					}
					var data = new Data();
					if(IsZeroSized == false)
					{
						var source = sources[i];
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out SafeZoneSyncData_DataProcessor dataProcessor)
		{
			dataProcessor = new SafeZoneSyncData_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<Arena.Server.SafeZoneSyncData>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct DifficultyData_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<Arena.DifficultyData> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<DifficultyData_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			Arena.DifficultyData sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new Arena.DifficultyData();
			}
			sourceData.EnemyStrengthMultiplier = data.EnemyStrengthMultiplier;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class DifficultyData_Sync : ComponentDataSync<DifficultyData_DataProcessor, DifficultyData_Sync.Data, DifficultyData_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(DifficultyData_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<Arena.DifficultyData>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.UInt16 EnemyStrengthMultiplier;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<Arena.DifficultyData>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<Arena.DifficultyData> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<Arena.DifficultyData> sources = default;
				if(IsZeroSized == false)
				{
					sources = chunk.GetNativeArray(SourceType);
				}
				for(int i=0; i<chunk.Count; i++)
				{
					var networkId = netIds[i];
					if(networkId.IsValid == false)
					{
						continue;
					}
					var data = new Data();
					if(IsZeroSized == false)
					{
						var source = sources[i];
						data.EnemyStrengthMultiplier = source.EnemyStrengthMultiplier;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out DifficultyData_DataProcessor dataProcessor)
		{
			dataProcessor = new DifficultyData_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<Arena.DifficultyData>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct SceneSectionState_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public BufferLookup<Arena.GameSceneCode.SceneSectionState> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			DynamicBuffer<Arena.GameSceneCode.SceneSectionState> dataBuffer;
			if(hasComponent)
			{
				dataBuffer = options.Commands.SetBuffer<Arena.GameSceneCode.SceneSectionState>(entity);
			}
			else
			{
				dataBuffer = options.Commands.AddBuffer<Arena.GameSceneCode.SceneSectionState>(entity);
			}
			var streamReader = new DataStreamReader(bytes);
			var reader = new ReadStream(ref streamReader);
			while(reader.CanReadBytes<SceneSectionState_Sync.Data>())
			{
				var data = reader.ReadStruct<SceneSectionState_Sync.Data>();
				var sourceData = new Arena.GameSceneCode.SceneSectionState();
				sourceData.SectionIndex = data.SectionIndex;
				sourceData.ShouldBeLoaded = data.ShouldBeLoaded;
				dataBuffer.Add(sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class SceneSectionState_Sync : DynamicBufferDataSync<SceneSectionState_DataProcessor, SceneSectionState_Sync.Data, SceneSectionState_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(SceneSectionState_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<Arena.GameSceneCode.SceneSectionState>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.Int32 SectionIndex;
			public System.Boolean ShouldBeLoaded;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetBufferTypeHandle<Arena.GameSceneCode.SceneSectionState>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public BufferTypeHandle<Arena.GameSceneCode.SceneSectionState> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				BufferAccessor<Arena.GameSceneCode.SceneSectionState> sources = default;
				sources = chunk.GetBufferAccessor(SourceType);
				for(int i=0; i<chunk.Count; i++)
				{
					var networkId = netIds[i];
					if(networkId.IsValid == false)
					{
						continue;
					}
					var sourceBuffer = sources[i];
					var tmpDataArray = new NativeArray<Data>(sourceBuffer.Length, Allocator.Temp);
					for(int k=0; k<sourceBuffer.Length; k++)
					{
						var data = new Data();
						var source = sourceBuffer[k];
						data.SectionIndex = source.SectionIndex;
						data.ShouldBeLoaded = source.ShouldBeLoaded;
						tmpDataArray[k] = data;
					}
					DataMap.WriteNativeArray(networkId, tmpDataArray);
					tmpDataArray.Dispose();
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out SceneSectionState_DataProcessor dataProcessor)
		{
			dataProcessor = new SceneSectionState_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetBufferLookup<Arena.GameSceneCode.SceneSectionState>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct CharacterClassData_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<Arena.CharacterClassData> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<CharacterClassData_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			Arena.CharacterClassData sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new Arena.CharacterClassData();
			}
			sourceData.Value = data.Value;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class CharacterClassData_Sync : ComponentDataSync<CharacterClassData_DataProcessor, CharacterClassData_Sync.Data, CharacterClassData_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(CharacterClassData_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<Arena.CharacterClassData>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public Arena.CharacterClass Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<Arena.CharacterClassData>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<Arena.CharacterClassData> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<Arena.CharacterClassData> sources = default;
				if(IsZeroSized == false)
				{
					sources = chunk.GetNativeArray(SourceType);
				}
				for(int i=0; i<chunk.Count; i++)
				{
					var networkId = netIds[i];
					if(networkId.IsValid == false)
					{
						continue;
					}
					var data = new Data();
					if(IsZeroSized == false)
					{
						var source = sources[i];
						data.Value = source.Value;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out CharacterClassData_DataProcessor dataProcessor)
		{
			dataProcessor = new CharacterClassData_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<Arena.CharacterClassData>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
}
