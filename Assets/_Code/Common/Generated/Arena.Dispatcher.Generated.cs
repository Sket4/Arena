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
				if (dataInfo.SerializatorID == 31)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 40)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.DifficultyData_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 38)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 20)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 24)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 28)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Level_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 7)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.ActivatedState_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 26)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.AbilityID_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 32)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.PredictedEntityData_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 10)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Group_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 8)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Consumable_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 9)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Droppable_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 25)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 13)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Name30_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 37)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Target_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 12)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.LivingState_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 27)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Health_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 11)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.Instigator_Sync.Tag());
				}
				else if (dataInfo.SerializatorID == 14)
				{
					commands.AddComponent(entity, new TzarGames.MultiplayerKit.Generated.XP_Sync.Tag());
				}
			}
			).Run();
		}

		public static bool GetID(System.Type type, out byte id)
		{
			if(type == typeof(TzarGames.MultiplayerKit.Generated.SceneSectionState_Sync))
			{
				id = 31; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.DifficultyData_Sync))
			{
				id = 40; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.SafeZoneSyncData_Sync))
			{
				id = 38; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.ArenaMatchStateData_Sync))
			{
				id = 20; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.CharacterClassData_Sync))
			{
				id = 24; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Level_Sync))
			{
				id = 28; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.ActivatedState_Sync))
			{
				id = 7; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.AbilityID_Sync))
			{
				id = 26; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.PredictedEntityData_Sync))
			{
				id = 32; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Group_Sync))
			{
				id = 10; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Consumable_Sync))
			{
				id = 8; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Droppable_Sync))
			{
				id = 9; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.AbilityOwner_Sync))
			{
				id = 25; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Name30_Sync))
			{
				id = 13; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Target_Sync))
			{
				id = 37; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.LivingState_Sync))
			{
				id = 12; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Health_Sync))
			{
				id = 27; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.Instigator_Sync))
			{
				id = 11; return true;
			}
			if(type == typeof(TzarGames.MultiplayerKit.Generated.XP_Sync))
			{
				id = 14; return true;
			}
			id = 0; return false;
		}

	}
}
