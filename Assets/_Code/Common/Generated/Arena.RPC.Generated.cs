using UnityEngine;
using Unity.Networking.Transport;
using System.Reflection;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TzarGames.MultiplayerKit.Generated
{
	class Arena_RpcHandler : IRpcHandler
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			NetworkRpcSystem.RegisterRpcHandler(new Arena_RpcHandler());
		}

		public bool GetRpcCode(System.Type rpcHandlerType, MethodInfo method, out RemoteCallInfo info)
		{
			if( typeof(Arena.IServerCorrectionSystem).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(10,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "CorrectPositionOnClient": info.MethodCode = 0; info.Channel = ChannelType.Unreliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(Arena.IInputSyncSystem).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(9,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "SendInputToServer": info.MethodCode = 0; info.Channel = ChannelType.Unreliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(Arena.IServerArenaCommands).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(0,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "RequestContinueGame": info.MethodCode = 2; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "NotifyExitingFromGame": info.MethodCode = 3; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(Arena.StoreSystem))
			{
				info = new RemoteCallInfo(13,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "PurchaseResultRPC": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "PurchaseItemRPC": info.MethodCode = 1; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "SellResultRPC": info.MethodCode = 2; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "SellItemRPC": info.MethodCode = 3; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(Arena.HitSyncSystem))
			{
				info = new RemoteCallInfo(15,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "SendHitsToClient": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			info = default;
			return false;
		}

		public bool Call(NetworkPlayer owner, Entity senderEntity, NetworkPlayer sender, byte handlerCode, byte rpcCode, INetworkObject target, bool isServer, ref DataStreamReader reader, EntityCommandBuffer commands)
		{
			if(handlerCode == 10)
			{
				if(target is Arena.IServerCorrectionSystem == false)
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
						System.Int32 inputCommandIndex = stream.ReadInt();
						Arena.CharacterContollerStateData controllerInternalData = stream.ReadStruct<Arena.CharacterContollerStateData>();
						(target as Arena.IServerCorrectionSystem).CorrectPositionOnClient(inputCommandIndex,controllerInternalData);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 9)
			{
				if(target is Arena.IInputSyncSystem == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						var stream = new ReadStream(ref reader);
						System.Byte[] inputData = (System.Byte[])stream.Read(typeof(System.Byte[]));
						TzarGames.MultiplayerKit.NetMessageInfo messageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as Arena.IInputSyncSystem).SendInputToServer(inputData,messageInfo);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 0)
			{
				if(target is Arena.IServerArenaCommands == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 2:
					{
						var stream = new ReadStream(ref reader);
						TzarGames.MultiplayerKit.NetMessageInfo info = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as Arena.IServerArenaCommands).RequestContinueGame(info);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 3:
					{
						var stream = new ReadStream(ref reader);
						System.Boolean requestMatchFinish = stream.ReadStruct<System.Boolean>();
						TzarGames.MultiplayerKit.NetMessageInfo info = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as Arena.IServerArenaCommands).NotifyExitingFromGame(requestMatchFinish,info);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 13)
			{
				if(target.GetType() != typeof(Arena.StoreSystem))
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
						Arena.PurchaseRequestStatus requestResult = stream.ReadStruct<Arena.PurchaseRequestStatus>();
						System.Guid requestGuid = stream.ReadStruct<System.Guid>();
						(target as Arena.StoreSystem).PurchaseResultRPC(requestResult,requestGuid);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 1:
					{
						var stream = new ReadStream(ref reader);
						Unity.Collections.NativeArray<Arena.PurchaseRequest_Item> itemsToPurchase;
						var arraySize = stream.ReadUShort();
						itemsToPurchase = new Unity.Collections.NativeArray<Arena.PurchaseRequest_Item>(arraySize, Unity.Collections.Allocator.Temp);
						unsafe
						{
							var size = sizeof(Arena.PurchaseRequest_Item) * arraySize;
							stream.ReadBytes((byte*)itemsToPurchase.GetUnsafePtr(), size);
						}

						TzarGames.MultiplayerKit.NetworkID storeNetId = stream.ReadStruct<NetworkID>();
						System.Guid requestGuid = stream.ReadStruct<System.Guid>();
						TzarGames.MultiplayerKit.NetMessageInfo netMessageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						try
						{
							(target as Arena.StoreSystem).PurchaseItemRPC(itemsToPurchase,storeNetId,requestGuid,netMessageInfo);
						}
						catch(System.Exception ex)
						{
							Debug.LogException(ex);
						}
						finally
						{
						};
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 2:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						Arena.SellRequestStatus result = stream.ReadStruct<Arena.SellRequestStatus>();
						System.Guid requestGuid = stream.ReadStruct<System.Guid>();
						(target as Arena.StoreSystem).SellResultRPC(result,requestGuid);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 3:
					{
						var stream = new ReadStream(ref reader);
						Unity.Collections.NativeArray<Arena.SellRequest_NetItem> itemsNetIdsToSell;
						var arraySize = stream.ReadUShort();
						itemsNetIdsToSell = new Unity.Collections.NativeArray<Arena.SellRequest_NetItem>(arraySize, Unity.Collections.Allocator.Temp);
						unsafe
						{
							var size = sizeof(Arena.SellRequest_NetItem) * arraySize;
							stream.ReadBytes((byte*)itemsNetIdsToSell.GetUnsafePtr(), size);
						}

						TzarGames.MultiplayerKit.NetworkID storeNetId = stream.ReadStruct<NetworkID>();
						System.Guid requestGuid = stream.ReadStruct<System.Guid>();
						TzarGames.MultiplayerKit.NetMessageInfo netMessageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						try
						{
							(target as Arena.StoreSystem).SellItemRPC(itemsNetIdsToSell,storeNetId,requestGuid,netMessageInfo);
						}
						catch(System.Exception ex)
						{
							Debug.LogException(ex);
						}
						finally
						{
						};
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 15)
			{
				if(target.GetType() != typeof(Arena.HitSyncSystem))
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
						Unity.Collections.NativeArray<Arena.HitSyncSystem.HitInfo> hitInfos;
						var arraySize = stream.ReadUShort();
						hitInfos = new Unity.Collections.NativeArray<Arena.HitSyncSystem.HitInfo>(arraySize, Unity.Collections.Allocator.Temp);
						unsafe
						{
							var size = sizeof(Arena.HitSyncSystem.HitInfo) * arraySize;
							stream.ReadBytes((byte*)hitInfos.GetUnsafePtr(), size);
						}


						try
						{
							(target as Arena.HitSyncSystem).SendHitsToClient(hitInfos,commands);
						}
						catch(System.Exception ex)
						{
							Debug.LogException(ex);
						}
						finally
						{
						};
						return true;
					}
					#endif
				}
			}
			return false;
		}
	}
}
