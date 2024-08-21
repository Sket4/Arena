using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;

[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Name30_DataProcessor,TzarGames.MultiplayerKit.Generated.Name30_Sync.Data,TzarGames.MultiplayerKit.Generated.Name30_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Name30_DataProcessor,TzarGames.MultiplayerKit.Generated.Name30_Sync.Data,TzarGames.MultiplayerKit.Generated.Name30_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.XP_DataProcessor,TzarGames.MultiplayerKit.Generated.XP_Sync.Data,TzarGames.MultiplayerKit.Generated.XP_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.XP_DataProcessor,TzarGames.MultiplayerKit.Generated.XP_Sync.Data,TzarGames.MultiplayerKit.Generated.XP_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Health_DataProcessor,TzarGames.MultiplayerKit.Generated.Health_Sync.Data,TzarGames.MultiplayerKit.Generated.Health_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Health_DataProcessor,TzarGames.MultiplayerKit.Generated.Health_Sync.Data,TzarGames.MultiplayerKit.Generated.Health_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.AbilityOwner_DataProcessor,TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync.Data,TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.AbilityOwner_DataProcessor,TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync.Data,TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Group_DataProcessor,TzarGames.MultiplayerKit.Generated.Group_Sync.Data,TzarGames.MultiplayerKit.Generated.Group_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Group_DataProcessor,TzarGames.MultiplayerKit.Generated.Group_Sync.Data,TzarGames.MultiplayerKit.Generated.Group_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Level_DataProcessor,TzarGames.MultiplayerKit.Generated.Level_Sync.Data,TzarGames.MultiplayerKit.Generated.Level_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Level_DataProcessor,TzarGames.MultiplayerKit.Generated.Level_Sync.Data,TzarGames.MultiplayerKit.Generated.Level_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.AbilityID_DataProcessor,TzarGames.GameCore.Abilities.AbilityID,TzarGames.MultiplayerKit.Generated.AbilityID_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.AbilityID_DataProcessor,TzarGames.GameCore.Abilities.AbilityID,TzarGames.MultiplayerKit.Generated.AbilityID_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.LivingState_DataProcessor,TzarGames.MultiplayerKit.Generated.LivingState_Sync.Data,TzarGames.MultiplayerKit.Generated.LivingState_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.LivingState_DataProcessor,TzarGames.MultiplayerKit.Generated.LivingState_Sync.Data,TzarGames.MultiplayerKit.Generated.LivingState_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Droppable_DataProcessor,TzarGames.MultiplayerKit.Generated.Droppable_Sync.Data,TzarGames.MultiplayerKit.Generated.Droppable_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Droppable_DataProcessor,TzarGames.MultiplayerKit.Generated.Droppable_Sync.Data,TzarGames.MultiplayerKit.Generated.Droppable_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.PredictedEntityData_DataProcessor,TzarGames.GameCore.PredictedEntitySyncData,TzarGames.MultiplayerKit.Generated.PredictedEntityData_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.PredictedEntityData_DataProcessor,TzarGames.GameCore.PredictedEntitySyncData,TzarGames.MultiplayerKit.Generated.PredictedEntityData_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Target_DataProcessor,TzarGames.MultiplayerKit.Generated.Target_Sync.Data,TzarGames.MultiplayerKit.Generated.Target_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Target_DataProcessor,TzarGames.MultiplayerKit.Generated.Target_Sync.Data,TzarGames.MultiplayerKit.Generated.Target_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.ActivatedState_DataProcessor,TzarGames.MultiplayerKit.Generated.ActivatedState_Sync.Data,TzarGames.MultiplayerKit.Generated.ActivatedState_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.ActivatedState_DataProcessor,TzarGames.MultiplayerKit.Generated.ActivatedState_Sync.Data,TzarGames.MultiplayerKit.Generated.ActivatedState_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Instigator_DataProcessor,TzarGames.MultiplayerKit.Generated.Instigator_Sync.Data,TzarGames.MultiplayerKit.Generated.Instigator_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Instigator_DataProcessor,TzarGames.MultiplayerKit.Generated.Instigator_Sync.Data,TzarGames.MultiplayerKit.Generated.Instigator_Sync.Tag>.SerializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Consumable_DataProcessor,TzarGames.MultiplayerKit.Generated.Consumable_Sync.Data,TzarGames.MultiplayerKit.Generated.Consumable_Sync.Tag>.DeserializeJob))]
[assembly: Unity.Jobs.RegisterGenericJobType(typeof(TzarGames.MultiplayerKit.ComponentDataSync<TzarGames.MultiplayerKit.Generated.Consumable_DataProcessor,TzarGames.MultiplayerKit.Generated.Consumable_Sync.Data,TzarGames.MultiplayerKit.Generated.Consumable_Sync.Tag>.SerializeJob))]

namespace TzarGames.MultiplayerKit.Generated
{
	public struct Name30_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Name30> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Name30_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Name30 sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Name30();
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
	public partial class Name30_Sync : ComponentDataSync<Name30_DataProcessor, Name30_Sync.Data, Name30_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Name30_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Name30>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = -100;
		}
		public struct Data
		{
			public Unity.Collections.FixedString64Bytes Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Name30>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Name30> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Name30> sources = default;
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
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Name30_DataProcessor dataProcessor)
		{
			dataProcessor = new Name30_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Name30>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct XP_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.XP> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<XP_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.XP sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.XP();
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
	public partial class XP_Sync : ComponentDataSync<XP_DataProcessor, XP_Sync.Data, XP_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(XP_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.XP>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.UInt32 Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.XP>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.XP> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.XP> sources = default;
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
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out XP_DataProcessor dataProcessor)
		{
			dataProcessor = new XP_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.XP>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Health_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Health> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Health_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Health sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Health();
			}
			sourceData.ActualHP = data.ActualHP;
			sourceData.ModifiedHP = data.ModifiedHP;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Health_Sync : ComponentDataSync<Health_DataProcessor, Health_Sync.Data, Health_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Health_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Health>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			SendInterval = 0.33f;
			Priority = 0;
		}
		public struct Data
		{
			public System.Single ActualHP;
			public System.Single ModifiedHP;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Health>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Health> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Health> sources = default;
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
						data.ActualHP = source.ActualHP;
						data.ModifiedHP = source.ModifiedHP;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Health_DataProcessor dataProcessor)
		{
			dataProcessor = new Health_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Health>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct AbilityOwner_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Abilities.AbilityOwner> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<AbilityOwner_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Abilities.AbilityOwner sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Abilities.AbilityOwner();
			}
			if(data.Value != NetworkID.Invalid && EntityFromNetworkId.TryGet(data.Value, out Entity Value))
			{
				sourceData.Value = Value;
			}
			else
			{
				sourceData.Value = Entity.Null;
			}
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class AbilityOwner_Sync : ComponentDataSync<AbilityOwner_DataProcessor, AbilityOwner_Sync.Data, AbilityOwner_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(AbilityOwner_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityOwner>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public NetworkID Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				NetworkIdFromEntity = system.GetComponentLookup<NetworkID>(true),
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityOwner> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			[ReadOnly] public ComponentLookup<NetworkID> NetworkIdFromEntity;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Abilities.AbilityOwner> sources = default;
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
						if(NetworkIdFromEntity.HasComponent(source.Value))
						{
							data.Value = NetworkIdFromEntity[source.Value];
						}
						else
						{
							data.Value = NetworkID.Invalid;
						}
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out AbilityOwner_DataProcessor dataProcessor)
		{
			dataProcessor = new AbilityOwner_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Abilities.AbilityOwner>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Group_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Group> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Group_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Group sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Group();
			}
			sourceData.ID = data.ID;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Group_Sync : ComponentDataSync<Group_DataProcessor, Group_Sync.Data, Group_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Group_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Group>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 50;
		}
		public struct Data
		{
			public System.Int16 ID;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Group>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Group> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Group> sources = default;
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
						data.ID = source.ID;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Group_DataProcessor dataProcessor)
		{
			dataProcessor = new Group_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Group>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Level_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Level> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Level_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Level sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Level();
			}
			sourceData.PreviousValue = data.PreviousValue;
			sourceData.Value = data.Value;
			sourceData.XpForCurrentLevel = data.XpForCurrentLevel;
			sourceData.XpForNextLevel = data.XpForNextLevel;
			sourceData.IsInitialized = data.IsInitialized;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Level_Sync : ComponentDataSync<Level_DataProcessor, Level_Sync.Data, Level_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Level_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Level>();
			var tagType = ComponentType.ReadOnly<TzarGames.GameCore.LevelSyncTag>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType, tagType);
			Priority = 0;
		}
		public struct Data
		{
			public System.UInt16 PreviousValue;
			public System.UInt16 Value;
			public System.UInt32 XpForCurrentLevel;
			public System.UInt32 XpForNextLevel;
			public System.Boolean IsInitialized;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Level>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Level> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Level> sources = default;
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
						data.PreviousValue = source.PreviousValue;
						data.Value = source.Value;
						data.XpForCurrentLevel = source.XpForCurrentLevel;
						data.XpForNextLevel = source.XpForNextLevel;
						data.IsInitialized = source.IsInitialized;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Level_DataProcessor dataProcessor)
		{
			dataProcessor = new Level_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Level>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct AbilityID_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Abilities.AbilityID> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<TzarGames.GameCore.Abilities.AbilityID>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			var sourceData = data;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class AbilityID_Sync : ComponentDataSync<AbilityID_DataProcessor, TzarGames.GameCore.Abilities.AbilityID, AbilityID_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(AbilityID_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Abilities.AbilityID>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			SendOnce = true;
			Priority = 0;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Abilities.AbilityID> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Abilities.AbilityID> sources = default;
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
					var data = sources[i];
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out AbilityID_DataProcessor dataProcessor)
		{
			dataProcessor = new AbilityID_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Abilities.AbilityID>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct LivingState_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.LivingState> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<LivingState_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.LivingState sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.LivingState();
			}
			sourceData.IsAlive = data.IsAlive;
			if(options.IsJustCreated)
			{
				sourceData.InitializeAfterFirstTimeSync();
			}
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class LivingState_Sync : ComponentDataSync<LivingState_DataProcessor, LivingState_Sync.Data, LivingState_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(LivingState_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.LivingState>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.Boolean IsAlive;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.LivingState>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.LivingState> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.LivingState> sources = default;
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
						data.IsAlive = source.IsAlive;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out LivingState_DataProcessor dataProcessor)
		{
			dataProcessor = new LivingState_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.LivingState>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Droppable_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Droppable> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Droppable_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Droppable sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Droppable();
			}
			if(data.PreviousOwner != NetworkID.Invalid && EntityFromNetworkId.TryGet(data.PreviousOwner, out Entity PreviousOwner))
			{
				sourceData.PreviousOwner = PreviousOwner;
			}
			else
			{
				sourceData.PreviousOwner = Entity.Null;
			}
			sourceData.IsDropped = data.IsDropped;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Droppable_Sync : ComponentDataSync<Droppable_DataProcessor, Droppable_Sync.Data, Droppable_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Droppable_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Droppable>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public NetworkID PreviousOwner;
			public System.Boolean IsDropped;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Droppable>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				NetworkIdFromEntity = system.GetComponentLookup<NetworkID>(true),
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Droppable> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			[ReadOnly] public ComponentLookup<NetworkID> NetworkIdFromEntity;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Droppable> sources = default;
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
						if(NetworkIdFromEntity.HasComponent(source.PreviousOwner))
						{
							data.PreviousOwner = NetworkIdFromEntity[source.PreviousOwner];
						}
						else
						{
							data.PreviousOwner = NetworkID.Invalid;
						}
						data.IsDropped = source.IsDropped;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Droppable_DataProcessor dataProcessor)
		{
			dataProcessor = new Droppable_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Droppable>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct PredictedEntityData_DataProcessor : INetworkIdDataProcessor
	{
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
		}
	}
	[DisableAutoCreation]
	public partial class PredictedEntityData_Sync : ComponentDataSync<PredictedEntityData_DataProcessor, TzarGames.GameCore.PredictedEntitySyncData, PredictedEntityData_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(PredictedEntityData_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.PredictedEntityData>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.PredictedEntityData>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				NetworkIdFromEntity = system.GetComponentLookup<NetworkID>(true),
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.PredictedEntityData> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			[ReadOnly] public ComponentLookup<NetworkID> NetworkIdFromEntity;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.PredictedEntityData> sources = default;
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
					var data = new TzarGames.GameCore.PredictedEntitySyncData();
					if(IsZeroSized == false)
					{
						var source = sources[i];
						if(NetworkIdFromEntity.HasComponent(source.Spawner))
						{
							data.Spawner = NetworkIdFromEntity[source.Spawner];
						}
						else
						{
							data.Spawner = NetworkID.Invalid;
						}
						data.Index = source.Index;
						data.CommandIndex = source.CommandIndex;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out PredictedEntityData_DataProcessor dataProcessor)
		{
			dataProcessor = new PredictedEntityData_DataProcessor()
			{
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Target_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Target> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Target_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Target sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Target();
			}
			if(data.Value != NetworkID.Invalid && EntityFromNetworkId.TryGet(data.Value, out Entity Value))
			{
				sourceData.Value = Value;
			}
			else
			{
				sourceData.Value = Entity.Null;
			}
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Target_Sync : ComponentDataSync<Target_DataProcessor, Target_Sync.Data, Target_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Target_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Target>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public NetworkID Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Target>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				NetworkIdFromEntity = system.GetComponentLookup<NetworkID>(true),
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Target> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			[ReadOnly] public ComponentLookup<NetworkID> NetworkIdFromEntity;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Target> sources = default;
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
						if(NetworkIdFromEntity.HasComponent(source.Value))
						{
							data.Value = NetworkIdFromEntity[source.Value];
						}
						else
						{
							data.Value = NetworkID.Invalid;
						}
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Target_DataProcessor dataProcessor)
		{
			dataProcessor = new Target_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Target>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct ActivatedState_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.ActivatedState> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<ActivatedState_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.ActivatedState sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.ActivatedState();
			}
			sourceData.Activated = data.Activated;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class ActivatedState_Sync : ComponentDataSync<ActivatedState_DataProcessor, ActivatedState_Sync.Data, ActivatedState_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(ActivatedState_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.ActivatedState>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 50;
		}
		public struct Data
		{
			public System.Boolean Activated;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.ActivatedState>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.ActivatedState> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.ActivatedState> sources = default;
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
						data.Activated = source.Activated;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out ActivatedState_DataProcessor dataProcessor)
		{
			dataProcessor = new ActivatedState_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.ActivatedState>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Instigator_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Instigator> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Instigator_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Instigator sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Instigator();
			}
			if(data.Value != NetworkID.Invalid && EntityFromNetworkId.TryGet(data.Value, out Entity Value))
			{
				sourceData.Value = Value;
			}
			else
			{
				sourceData.Value = Entity.Null;
			}
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Instigator_Sync : ComponentDataSync<Instigator_DataProcessor, Instigator_Sync.Data, Instigator_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Instigator_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Instigator>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public NetworkID Value;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Instigator>(true),
				NetworkIdType = system.GetComponentTypeHandle<NetworkID>(true),
				DataMap = dataMap,
				NetworkIdFromEntity = system.GetComponentLookup<NetworkID>(true),
				IsZeroSized = sourceType.IsZeroSized
			};
			return job.Schedule(collectQuery, inputDeps);
		}
		[BurstCompile]
		struct CollectDataJob : IJobChunk
		{
			public SerializedDataContainer DataMap;
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Instigator> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			[ReadOnly] public ComponentLookup<NetworkID> NetworkIdFromEntity;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Instigator> sources = default;
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
						if(NetworkIdFromEntity.HasComponent(source.Value))
						{
							data.Value = NetworkIdFromEntity[source.Value];
						}
						else
						{
							data.Value = NetworkID.Invalid;
						}
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Instigator_DataProcessor dataProcessor)
		{
			dataProcessor = new Instigator_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Instigator>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
	public struct Consumable_DataProcessor : INetworkIdDataProcessor
	{
		[ReadOnly] public ComponentLookup<TzarGames.GameCore.Consumable> DataFromEntity;
		[ReadOnly] public EntityFromNetworkId EntityFromNetworkId;
		public void Deserialize(in NetworkID networkID, in NativeArray<byte> bytes, in NetworkIdDataProcessorOptions options)
		{
			var data = NetworkIdDataProcessorUtility.AsStruct<Consumable_Sync.Data>(bytes);
			if(EntityFromNetworkId.TryGet(networkID, out Entity entity) == false)
			{
				return;
			}
			var hasComponent = DataFromEntity.HasComponent(entity);
			TzarGames.GameCore.Consumable sourceData;
			if(hasComponent)
			{
				sourceData = DataFromEntity[entity];
			}
			else
			{
				sourceData = new TzarGames.GameCore.Consumable();
			}
			sourceData.Count = data.Count;
			if (hasComponent) options.Commands.SetComponent(entity, sourceData);
			else
			{
				options.Commands.AddComponent(entity, sourceData);
			}
		}
	}
	[DisableAutoCreation]
	public partial class Consumable_Sync : ComponentDataSync<Consumable_DataProcessor, Consumable_Sync.Data, Consumable_Sync.Tag>
	{
		ComponentType sourceType;
		EntityQuery applyQuery;
		EntityQuery collectQuery;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] static void Register()
		{
			DataSyncBase.RegisterDataSync(typeof(Consumable_Sync));
		}
		public override void Initialize(DataSyncSystemBase system)
		{
			base.Initialize(system);
			applyQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>());
			sourceType = ComponentType.ReadOnly<TzarGames.GameCore.Consumable>();
			collectQuery = system.GetEntityQueryForJob(ComponentType.ReadOnly<NetworkID>(), sourceType);
			Priority = 0;
		}
		public struct Data
		{
			public System.UInt32 Count;
		}
		public struct Tag : IComponentData
		{
		}
		protected override JobHandle ScheduleCollectDataJob(SerializedDataContainer dataMap, JobHandle inputDeps)
		{
			var job = new CollectDataJob()
			{
				SourceType = system.GetComponentTypeHandle<TzarGames.GameCore.Consumable>(true),
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
			[ReadOnly] public ComponentTypeHandle<TzarGames.GameCore.Consumable> SourceType;
			[ReadOnly] public ComponentTypeHandle<NetworkID> NetworkIdType;
			public bool IsZeroSized;
			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var netIds = chunk.GetNativeArray(NetworkIdType);
				NativeArray<TzarGames.GameCore.Consumable> sources = default;
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
						data.Count = source.Count;
					}
					DataMap.WriteStruct(networkId, ref data);
				}
			}
		}
		protected override JobHandle ScheduleRelevancyJob(SerializedDataContainer dataMap, NativeParallelMultiHashMap<int, int> relevancyMap, JobHandle inputDeps)
		{
			return inputDeps;
		}
		protected override JobHandle CreateDataProcessor(JobHandle inputDeps, out Consumable_DataProcessor dataProcessor)
		{
			dataProcessor = new Consumable_DataProcessor()
			{
				EntityFromNetworkId = new EntityFromNetworkId(system),
				DataFromEntity = system.GetComponentLookup<TzarGames.GameCore.Consumable>(true)
			};
			return inputDeps;
		}
		protected override bool UseRelevancy()
		{
			return false;
		}
	}
}
