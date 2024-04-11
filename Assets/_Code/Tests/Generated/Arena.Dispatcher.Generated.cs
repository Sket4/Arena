using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace TzarGames.MultiplayerKit.Generated
{
	[DisableAutoCreation]
	partial class Arena_Dispatcher : SystemBase, ISerializedDataDispatcher
	{
		public DataDispatcherSystem DispatcherSystem { get; set; }
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			DataDispatcherSystem.AddDispatcherType<Arena_Dispatcher>(GetID);
		}

		protected override void OnUpdate()
		{
			var commands = DispatcherSystem.CommandBufferForDispacher;
			Entities.ForEach((Entity entity, in SerializedDataInfo dataInfo) => 
			{
				if(dataInfo.IsProcessed)
				{
					return;
				}
				if (dataInfo.SerializatorID == 30)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 2)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 1)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync.Tag());
				}
			}
			).Run();
		}

		public static bool GetID(System.Type type, out byte id)
		{
			if(type == typeof(TzarGames.MultiplayerKit.Generated.TestAutoGenDynamicBufferStructure_Sync))
			{
				id = 30; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.TestEmptyDataStructure_Sync))
			{
				id = 2; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.TestDataStructure_Sync))
			{
				id = 1; return true;
			}
			id = 0; return false;
		}

	}
}
