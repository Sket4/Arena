using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

namespace TzarGames.MultiplayerKit.Generated
{
	[DisableAutoCreation]
	partial class TzarGames_Dispatcher : SystemBase, ISerializedDataDispatcher
	{
		public DataDispatcherSystem DispatcherSystem { get; set; }
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			DataDispatcherSystem.AddDispatcherType<TzarGames_Dispatcher>(GetID);
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
				if (dataInfo.SerializatorID == 6)
				{
					commands.AddComponent(entity, new TzarGames.Common.NetworkPlayerSyncTag());
				}
				else if (dataInfo.SerializatorID == 35)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.DeathDataNetSyncTag());
				}
				else if (dataInfo.SerializatorID == 21)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.ItemNetSyncTag());
				}
				else if (dataInfo.SerializatorID == 22)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.ItemOwnerSyncTag());
				}
				else if (dataInfo.SerializatorID == 23)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.PrefabIdSyncTag());
				}
				else if (dataInfo.SerializatorID == 17)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.NetworkMovementSyncMessage());
				}
				else if (dataInfo.SerializatorID == 18)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.TranslationNetSyncTag());
				}
				else if (dataInfo.SerializatorID == 36)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.ScriptViz.ScriptVizDataNetSyncTag());
				}
				else if (dataInfo.SerializatorID == 19)
				{
					commands.AddComponent(entity, new TzarGames.GameCore.Abilities.Networking.AbilityStateNetSyncDataTag());
				}
			}
			).Run();
		}

		public static bool GetID(System.Type type, out byte id)
		{
			if(type == typeof(TzarGames.Common.NetworkPlayerDataSync))
			{
				id = 6; return true;
			}
			if(type == typeof(TzarGames.GameCore.DeathDataSync))
			{
				id = 35; return true;
			}
			if(type == typeof(TzarGames.GameCore.ItemCreationDataSync))
			{
				id = 21; return true;
			}
			if(type == typeof(TzarGames.GameCore.ItemOwnerDataSync))
			{
				id = 22; return true;
			}
			if(type == typeof(TzarGames.GameCore.PrefabIdDataSync))
			{
				id = 23; return true;
			}
			if(type == typeof(TzarGames.GameCore.SmoothMovementNetSync))
			{
				id = 17; return true;
			}
			if(type == typeof(TzarGames.GameCore.TranslationDataSync))
			{
				id = 18; return true;
			}
			if(type == typeof(TzarGames.GameCore.ScriptViz.ScriptVizDataSync))
			{
				id = 36; return true;
			}
			if(type == typeof(TzarGames.GameCore.Abilities.Networking.AbilityStateNetSync))
			{
				id = 19; return true;
			}
			id = 0; return false;
		}

	}
}
