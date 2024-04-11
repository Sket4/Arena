using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace TzarGames.MultiplayerKit.Generated
{
	[DisableAutoCreation]
	partial class MultiplayerKit_Dispatcher : SystemBase, ISerializedDataDispatcher
	{
		public DataDispatcherSystem DispatcherSystem { get; set; }
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			DataDispatcherSystem.AddDispatcherType<MultiplayerKit_Dispatcher>(GetID);
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
				if (dataInfo.SerializatorID == 3)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Tests.TestDataTag());
				}
				else if (dataInfo.SerializatorID == 29)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Tests.TestDynamicBufferDataTag());
				}
				else if (dataInfo.SerializatorID == 5)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Tests.TestRelevancyDataTag());
				}
				else if (dataInfo.SerializatorID == 4)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Tests.TestStateTag());
				}
			}
			).Run();
		}

		public static bool GetID(System.Type type, out byte id)
		{
			if(type == typeof(TzarGames.MultiplayerKit.Tests.TestDataSync))
			{
				id = 3; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Tests.TestDynamicBufferSync))
			{
				id = 29; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Tests.TestRelevancyDataSync))
			{
				id = 5; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Tests.TestStateDataSync))
			{
				id = 4; return true;
			}
			id = 0; return false;
		}

	}
}
