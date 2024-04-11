using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;

[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.DynamicBufferDataSync<TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.DynamicBufferDataSync<TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.TestDataStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.TestDataStructure_DataProcessor,TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync.Data,TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync.Tag>.SerializeJob))]

namespace TzarGames.MultiplayerKit.Generated
{
	public struct TestAutoGenDynamicBufferStructure_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public BufferLookup<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			DynamicBuffer<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure> dataBuffer;
			if(hasComponent)
			{
				dataBuffer = options.Commands.SetBuffer<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure>(entity);
			}
			else
			{
				dataBuffer = options.Commands.AddBuffer<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure>(entity);
			}
			var streamReader = new DataStreamReader(bytes);
			var reader = new ReadStream(ref streamReader);
			while(reader.CanReadBytes<TestAutoGenDynamicBufferStructure_Sync.Data>())
			{
				var data = reader.ReadStruct<TestAutoGenDynamicBufferStructure_Sync.Data>();
				var sourceData = new TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure();
				sourceData.Int = data.Int;
				dataBuffer.Add(sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class TestAutoGenDynamicBufferStructure_Sync : DynamicBufferDataSync<TestAutoGenDynamicBufferStructure_DataProcessor, TestAutoGenDynamicBufferStructure_Sync.Data, TestAutoGenDynamicBufferStructure_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(TestAutoGenDynamicBufferStructure_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.Int32 Int;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetBufferTypeHandle<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure>(true),
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
			[ReadOnly] public BufferTypeHandle<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				BufferAccessor<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure> sources = default;
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
						data.Int = source.Int;
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
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out TestAutoGenDynamicBufferStructure_DataProcessor dataProcessor)
		{
			dataProcessor = new TestAutoGenDynamicBufferStructure_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetBufferLookup<TzarGames.MultiplayerKit.Tests.TestAutoGenDynamicBufferStructure>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct TestEmptyDataStructure_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<TestEmptyDataStructure_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure sourceData;
			sourceData = default;
			if (hasComponent == false) options.Commands.SetComponent(entity, sourceData);
		}
	}
	[DisableAutoCreation]
	public partial class TestEmptyDataStructure_Sync : ComponentDataSync<TestEmptyDataStructure_DataProcessor, TestEmptyDataStructure_Sync.Data, TestEmptyDataStructure_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(TestEmptyDataStructure_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure>();
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
				SourceType = system.GetComponentTypeHandle<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure> sources = default;
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
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out TestEmptyDataStructure_DataProcessor dataProcessor)
		{
			dataProcessor = new TestEmptyDataStructure_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.MultiplayerKit.Tests.TestEmptyDataStructure>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct TestDataStructure_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.MultiplayerKit.Tests.TestDataStructure> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<TestDataStructure_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.MultiplayerKit.Tests.TestDataStructure sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.MultiplayerKit.Tests.TestDataStructure();
			}
			sourceData.Float = data.Float;
			sourceData.Int = data.Int;
			sourceData.Byte = data.Byte;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class TestDataStructure_Sync : ComponentDataSync<TestDataStructure_DataProcessor, TestDataStructure_Sync.Data, TestDataStructure_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(TestDataStructure_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.MultiplayerKit.Tests.TestDataStructure>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.Single Float;
			public System.Int32 Int;
			public System.Byte Byte;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.MultiplayerKit.Tests.TestDataStructure>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.MultiplayerKit.Tests.TestDataStructure> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.MultiplayerKit.Tests.TestDataStructure> sources = default;
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
						data.Float = source.Float;
						data.Int = source.Int;
						data.Byte = source.Byte;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out TestDataStructure_DataProcessor dataProcessor)
		{
			dataProcessor = new TestDataStructure_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.MultiplayerKit.Tests.TestDataStructure>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
}
