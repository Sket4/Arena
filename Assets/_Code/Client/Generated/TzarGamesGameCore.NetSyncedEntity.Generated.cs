using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace TzarGames.MultiplayerKit.Generated
{
	[DisableAutoCreation]
	public partial class TzarGamesGameCore_SyncedNetEntityCreationSystem : Client.SyncedNetEntityCreationSystemBase
	{
		EntityQuery netIdsQuery;
		EntityQuery dataQuery;
		BufferTypeHandle<NetworkIdElement> _netIdElementTypeHandle;
		BufferTypeHandle<NetDataElement> _netDataElementTypeHandle;
		ComponentTypeHandle<NetworkID> _netIdTypeHandle;
		ComponentTypeHandle<SerializedDataInfo> _dataTypeHandle;
		EntityTypeHandle _entityTypeHandle;
		MainJob mainJob;
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		static void registerSystemType()
		{
			Client.SerializedDataProcessSystemGroup.RegisterProcessorSystemType<TzarGames.GameCore.Client.PredictedEntityCreationJob,TzarGamesGameCore_SyncedNetEntityCreationSystem>();
			Client.SerializedDataProcessSystemGroup.RegisterProcessorSystemType<TzarGames.GameCore.Client.NetSyncAbilityCreationJob,TzarGamesGameCore_SyncedNetEntityCreationSystem>();
			Client.SerializedDataProcessSystemGroup.RegisterProcessorSystemType<TzarGames.GameCore.Client.NetSyncEntityCreationJob,TzarGamesGameCore_SyncedNetEntityCreationSystem>();
			Client.SerializedDataProcessSystemGroup.RegisterProcessorSystemType<TzarGames.GameCore.Client.NetSyncItemCreationJob,TzarGamesGameCore_SyncedNetEntityCreationSystem>();
		}
		protected override void OnCreate()
		{
			base.OnCreate();
			netIdsQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkID>());
			dataQuery = GetEntityQuery(ComponentType.ReadWrite<SerializedDataInfo>(), ComponentType.ReadOnly<NetworkIdElement>(), ComponentType.ReadOnly<NetDataElement>());
			mainJob.job0 = new TzarGames.GameCore.Client.PredictedEntityCreationJob();
			mainJob.job0.OnCreate(this);
			mainJob.netIdElementTypeHandle = GetBufferTypeHandle<NetworkIdElement>(true);
			mainJob.netDataElementTypeHandle = GetBufferTypeHandle<NetDataElement>(true);
			mainJob.netIdTypeHandle = GetComponentTypeHandle<NetworkID>(true);
			mainJob.dataTypeHandle = GetComponentTypeHandle<SerializedDataInfo>();
			mainJob.entityTypeHandle = GetEntityTypeHandle();
			mainJob.job1 = new TzarGames.GameCore.Client.NetSyncAbilityCreationJob();
			mainJob.job1.OnCreate(this);
			mainJob.netIdElementTypeHandle = GetBufferTypeHandle<NetworkIdElement>(true);
			mainJob.netDataElementTypeHandle = GetBufferTypeHandle<NetDataElement>(true);
			mainJob.netIdTypeHandle = GetComponentTypeHandle<NetworkID>(true);
			mainJob.dataTypeHandle = GetComponentTypeHandle<SerializedDataInfo>();
			mainJob.entityTypeHandle = GetEntityTypeHandle();
			mainJob.job2 = new TzarGames.GameCore.Client.NetSyncEntityCreationJob();
			mainJob.job2.OnCreate(this);
			mainJob.netIdElementTypeHandle = GetBufferTypeHandle<NetworkIdElement>(true);
			mainJob.netDataElementTypeHandle = GetBufferTypeHandle<NetDataElement>(true);
			mainJob.netIdTypeHandle = GetComponentTypeHandle<NetworkID>(true);
			mainJob.dataTypeHandle = GetComponentTypeHandle<SerializedDataInfo>();
			mainJob.entityTypeHandle = GetEntityTypeHandle();
			mainJob.job3 = new TzarGames.GameCore.Client.NetSyncItemCreationJob();
			mainJob.job3.OnCreate(this);
			mainJob.netIdElementTypeHandle = GetBufferTypeHandle<NetworkIdElement>(true);
			mainJob.netDataElementTypeHandle = GetBufferTypeHandle<NetDataElement>(true);
			mainJob.netIdTypeHandle = GetComponentTypeHandle<NetworkID>(true);
			mainJob.dataTypeHandle = GetComponentTypeHandle<SerializedDataInfo>();
			mainJob.entityTypeHandle = GetEntityTypeHandle();
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();
			mainJob.job0.OnDestroy(this);
			mainJob.job1.OnDestroy(this);
			mainJob.job2.OnDestroy(this);
			mainJob.job3.OnDestroy(this);
		}
		protected override void OnUpdate()
		{
			var commands = CommandBufferSystem.CreateCommandBuffer();
			var alreadyCreatedNetIds = SystemGroup.CreatedNetworkIds;
			var netIdChunks = netIdsQuery.ToArchetypeChunkArray(World.UpdateAllocator.ToAllocator);
			var dataChunks = dataQuery.ToArchetypeChunkArray(World.UpdateAllocator.ToAllocator);
			mainJob.netIdElementTypeHandle.Update(this);
			mainJob.netDataElementTypeHandle.Update(this);
			mainJob.netIdTypeHandle.Update(this);
			mainJob.dataTypeHandle.Update(this);
			mainJob.entityTypeHandle.Update(this);
			Dependency = mainJob.job0.OnBeforeUpdate(this, Dependency);
			Dependency = mainJob.job1.OnBeforeUpdate(this, Dependency);
			Dependency = mainJob.job2.OnBeforeUpdate(this, Dependency);
			Dependency = mainJob.job3.OnBeforeUpdate(this, Dependency);
			mainJob.enableJournaling = EnableDebugJournaling;
			mainJob.debugRecordArchetype = DebugJournalRecordArchetype;

			mainJob.commands = commands;
			mainJob.alreadyCreatedNetIds = alreadyCreatedNetIds;
			mainJob.netIdChunks = netIdChunks;
			mainJob.dataChunks = dataChunks;
			mainJob.Run();
			mainJob.job1.Dispose();
			mainJob.job2.Dispose();
			mainJob.job3.Dispose();
		}
	}
	[BurstCompile]
	struct MainJob : IJob
	{
		public bool enableJournaling;
		public EntityArchetype debugRecordArchetype;
		public EntityCommandBuffer commands;
		public NativeList<NetworkID> alreadyCreatedNetIds;
		[ReadOnly] public NativeArray<ArchetypeChunk> netIdChunks;
		[ReadOnly] public NativeArray<ArchetypeChunk> dataChunks;
		[ReadOnly] public ComponentTypeHandle<NetworkID> netIdTypeHandle;
		public ComponentTypeHandle<SerializedDataInfo> dataTypeHandle;
		[ReadOnly] public EntityTypeHandle entityTypeHandle;
		[ReadOnly] public BufferTypeHandle<NetworkIdElement> netIdElementTypeHandle;
		[ReadOnly] public BufferTypeHandle<NetDataElement> netDataElementTypeHandle;
		public TzarGames.GameCore.Client.PredictedEntityCreationJob job0;
		public TzarGames.GameCore.Client.NetSyncAbilityCreationJob job1;
		public TzarGames.GameCore.Client.NetSyncEntityCreationJob job2;
		public TzarGames.GameCore.Client.NetSyncItemCreationJob job3;

		public void Execute()
		{
			// calculate total data count
			int totalDataCount = 0;

			for (int c=0; c < dataChunks.Length; c++)
			{
				var chunk = dataChunks[c];
				var dataInfos = chunk.GetNativeArray(ref dataTypeHandle);
				for (int i = 0; i < chunk.Count; i++)
				{
					var nextDataInfo = dataInfos[i];
					if(nextDataInfo.IsProcessed)
					{
						continue;
					}
					if(IsCreatorID(nextDataInfo.SerializatorID, out int priority))
					{
						totalDataCount++;
					}
				}
			}

			if(totalDataCount == 0)
			{
				return;
			}

			// collect data info and sort
			var dataInfoList = new NativeArray<Client.SerializedDataPriorityInfo>(totalDataCount, Allocator.Temp);
			int dataCounter = 0;
			for (int c=0; c < dataChunks.Length; c++)
			{
				var chunk = dataChunks[c];
				var dataInfos = chunk.GetNativeArray(ref dataTypeHandle);
				for (int i = 0; i < chunk.Count; i++)
				{
					var nextDataInfo = dataInfos[i];
					if(nextDataInfo.IsProcessed)
					{
						continue;
					}
					if(IsCreatorID(nextDataInfo.SerializatorID, out int priority))
					{
						dataInfoList[dataCounter] = new Client.SerializedDataPriorityInfo
						{
							Priority = priority,
							ChunkIndex = c,
							ArrayIndex = i
						};
						dataCounter++;
					}
				}
			}
			dataInfoList.Sort(new Client.SerializedDataPriorityInfoComparer());

			foreach(var dataPriorityInfo in dataInfoList)
			{
				var chunk = dataChunks[dataPriorityInfo.ChunkIndex];
				var dataArray = chunk.GetNativeArray(ref dataTypeHandle);
				var dataInfo = dataArray[dataPriorityInfo.ArrayIndex];
				if(dataInfo.IsProcessed)
				{
					continue;
				}
				dataInfo.IsProcessed = true;
				dataArray[dataPriorityInfo.ArrayIndex] = dataInfo;
				var entity = chunk.GetNativeArray(entityTypeHandle)[dataPriorityInfo.ArrayIndex];
				commands.DestroyEntity(entity);
				var data = chunk.GetBufferAccessor(ref netDataElementTypeHandle)[dataPriorityInfo.ArrayIndex];
				var networkIds = chunk.GetBufferAccessor(ref netIdElementTypeHandle)[dataPriorityInfo.ArrayIndex];
				if (dataInfo.SerializatorID == 32)
				{
					Client.SyncedNetEntityCreationSystemBase.ProcessNetSyncedEntity<TzarGames.GameCore.Client.PredictedEntityCreationJob, TzarGames.GameCore.PredictedEntitySyncData>(job0, data, networkIds, ref commands, ref alreadyCreatedNetIds, in netIdChunks, netIdTypeHandle, enableJournaling, debugRecordArchetype, dataInfo);
				}
				if (dataInfo.SerializatorID == 26)
				{
					Client.SyncedNetEntityCreationSystemBase.ProcessNetSyncedEntity<TzarGames.GameCore.Client.NetSyncAbilityCreationJob, TzarGames.GameCore.Abilities.AbilityID>(job1, data, networkIds, ref commands, ref alreadyCreatedNetIds, in netIdChunks, netIdTypeHandle, enableJournaling, debugRecordArchetype, dataInfo);
				}
				if (dataInfo.SerializatorID == 23)
				{
					Client.SyncedNetEntityCreationSystemBase.ProcessNetSyncedEntity<TzarGames.GameCore.Client.NetSyncEntityCreationJob, TzarGames.GameCore.PrefabIdNetSyncData>(job2, data, networkIds, ref commands, ref alreadyCreatedNetIds, in netIdChunks, netIdTypeHandle, enableJournaling, debugRecordArchetype, dataInfo);
				}
				if (dataInfo.SerializatorID == 21)
				{
					Client.SyncedNetEntityCreationSystemBase.ProcessNetSyncedEntity<TzarGames.GameCore.Client.NetSyncItemCreationJob, TzarGames.GameCore.ItemNetSyncCreationData>(job3, data, networkIds, ref commands, ref alreadyCreatedNetIds, in netIdChunks, netIdTypeHandle, enableJournaling, debugRecordArchetype, dataInfo);
				}
			}
			dataInfoList.Dispose();
		}

		static bool IsCreatorID(int id, out int priority)
		{
			// TzarGames.GameCore.PredictedEntitySyncData
			if(id == 32)
			{
				priority = 4;
				return true;
			}
			// TzarGames.GameCore.Abilities.AbilityID
			else if(id == 26)
			{
				priority = 3;
				return true;
			}
			// TzarGames.GameCore.PrefabIdNetSyncData
			else if(id == 23)
			{
				priority = 2;
				return true;
			}
			// TzarGames.GameCore.ItemNetSyncCreationData
			else if(id == 21)
			{
				priority = 1;
				return true;
			}
			priority = default;
			return false;
		}
	}
}
