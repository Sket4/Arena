using UnityEngine;
using Unity.Networking.Transport;
using System.Reflection;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TzarGames.MultiplayerKit.Generated
{
	class TzarGames_RpcHandler : IRpcHandler
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			NetworkRpcSystem.RegisterRpcHandler(new TzarGames_RpcHandler());
		}

		public bool GetRpcCode(System.Type rpcHandlerType, MethodInfo method, out RemoteCallInfo info)
		{
			if( typeof(TzarGames.GameCore.IAbilityStateNetSync).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(7,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "NotifyAbilityStarted": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.SendToAllExceptTarget; info.RepeatCount = 1; return true;
					case "NotifyAbilityStopped": info.MethodCode = 1; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.SendToAllExceptTarget; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(TzarGames.GameCore.ActivateItemRequestSystem))
			{
				info = new RemoteCallInfo(16,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "OnServer_ActivateItem": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(TzarGames.GameCore.INetworkSceneLoadingRpc).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(12,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "NotifySceneLoadingStateChanged": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(TzarGames.GameCore.IItemTakenRpcHandler).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(8,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "NotifyDroppedItemTaken": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(TzarGames.GameCore.IPathMovementServerNetSync).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(6,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "Move": info.MethodCode = 0; info.Channel = ChannelType.Unreliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(TzarGames.GameCore.LevelSystem))
			{
				info = new RemoteCallInfo(11,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "OnLevelUpEvent": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(TzarGames.GameCore.MessageDispatcherSystem))
			{
				info = new RemoteCallInfo(14,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "SendMessage": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			info = default;
			return false;
		}

		public bool Call(NetworkPlayer owner, Entity senderEntity, NetworkPlayer sender, byte handlerCode, byte rpcCode, INetworkObject target, bool isServer, ref DataStreamReader reader, EntityCommandBuffer commands)
		{
			if(handlerCode == 7)
			{
				if(target is TzarGames.GameCore.IAbilityStateNetSync == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if !UNITY_SERVER
					case 0:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						TzarGames.GameCore.Abilities.AbilityID abilityID = stream.ReadStruct<TzarGames.GameCore.Abilities.AbilityID>();
						TzarGames.MultiplayerKit.NetworkID abilityOwnerNetID = stream.ReadStruct<NetworkID>();
						TzarGames.MultiplayerKit.NetworkID abilityOwnerTarget = stream.ReadStruct<NetworkID>();
						(target as TzarGames.GameCore.IAbilityStateNetSync).NotifyAbilityStarted(abilityID,abilityOwnerNetID,abilityOwnerTarget);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 1:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						TzarGames.GameCore.Abilities.AbilityID abilityID = stream.ReadStruct<TzarGames.GameCore.Abilities.AbilityID>();
						TzarGames.MultiplayerKit.NetworkID abilityOwnerNetID = stream.ReadStruct<NetworkID>();
						(target as TzarGames.GameCore.IAbilityStateNetSync).NotifyAbilityStopped(abilityID,abilityOwnerNetID);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 16)
			{
				if(target.GetType() != typeof(TzarGames.GameCore.ActivateItemRequestSystem))
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						var stream = new ReadStream(ref reader);
						TzarGames.MultiplayerKit.NetworkID itemNetId = stream.ReadStruct<NetworkID>();
						System.Boolean activate = stream.ReadStruct<System.Boolean>();
						TzarGames.MultiplayerKit.NetMessageInfo callData = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as TzarGames.GameCore.ActivateItemRequestSystem).OnServer_ActivateItem(itemNetId,activate,callData);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 12)
			{
				if(target is TzarGames.GameCore.INetworkSceneLoadingRpc == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						var stream = new ReadStream(ref reader);
						TzarGames.GameCore.PrefabID sceneId = stream.ReadStruct<TzarGames.GameCore.PrefabID>();
						System.Boolean isLoaded = stream.ReadStruct<System.Boolean>();
						TzarGames.MultiplayerKit.NetMessageInfo messageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as TzarGames.GameCore.INetworkSceneLoadingRpc).NotifySceneLoadingStateChanged(sceneId,isLoaded,messageInfo);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 8)
			{
				if(target is TzarGames.GameCore.IItemTakenRpcHandler == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if !UNITY_SERVER
					case 0:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						System.Int32 itemId = stream.ReadInt();
						Unity.Mathematics.float3 position = stream.ReadFloat3();
						System.UInt32 count = stream.ReadUInt();
						(target as TzarGames.GameCore.IItemTakenRpcHandler).NotifyDroppedItemTaken(itemId,position,count);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 6)
			{
				if(target is TzarGames.GameCore.IPathMovementServerNetSync == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						var stream = new ReadStream(ref reader);
						TzarGames.GameCore.ServerMoveInfo moveInfo = stream.ReadStruct<TzarGames.GameCore.ServerMoveInfo>();
						TzarGames.MultiplayerKit.NetMessageInfo netMessageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as TzarGames.GameCore.IPathMovementServerNetSync).Move(moveInfo,netMessageInfo);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 11)
			{
				if(target.GetType() != typeof(TzarGames.GameCore.LevelSystem))
				{
					return false;
				}
				switch(rpcCode)
				{
					#if !UNITY_SERVER
					case 0:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						TzarGames.MultiplayerKit.NetworkID networkID = stream.ReadStruct<NetworkID>();
						System.UInt16 prevLevel = stream.ReadUShort();
						System.UInt16 currentLevel = stream.ReadUShort();
						(target as TzarGames.GameCore.LevelSystem).OnLevelUpEvent(networkID,prevLevel,currentLevel);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 14)
			{
				if(target.GetType() != typeof(TzarGames.GameCore.MessageDispatcherSystem))
				{
					return false;
				}
				switch(rpcCode)
				{
					#if !UNITY_SERVER
					case 0:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						TzarGames.GameCore.Message message = stream.ReadStruct<TzarGames.GameCore.Message>();

						(target as TzarGames.GameCore.MessageDispatcherSystem).SendMessage(message,commands);
						return true;
					}
					#endif
				}
			}
			return false;
		}
	}
}
