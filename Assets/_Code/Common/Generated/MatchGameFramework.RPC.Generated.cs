using UnityEngine;

using System.Reflection;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TzarGames.MultiplayerKit.Generated
{
	class MatchGameFramework_RpcHandler : IRpcHandler
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			NetworkRpcSystem.RegisterRpcHandler(new MatchGameFramework_RpcHandler());
		}

		public bool GetRpcCode(System.Type rpcHandlerType, MethodInfo method, out RemoteCallInfo info)
		{
			if( typeof(TzarGames.MatchFramework.IServerMatchSystem).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(3,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "Authorize": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if( typeof(TzarGames.MatchFramework.IClientMatchSystem).IsAssignableFrom(rpcHandlerType))
			{
				info = new RemoteCallInfo(2,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "AuthError": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			info = default;
			return false;
		}

		public bool Call(NetworkPlayer owner, Entity senderEntity, NetworkPlayer sender, byte handlerCode, byte rpcCode, INetworkObject target, bool isServer, ref DataStreamReader reader, EntityCommandBuffer commands)
		{
			if(handlerCode == 3)
			{
				if(target is TzarGames.MatchFramework.IServerMatchSystem == false)
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						var stream = new ReadStream(ref reader);
						System.Byte[] encryptedToken = (System.Byte[])stream.Read(typeof(System.Byte[]));
						TzarGames.MultiplayerKit.NetMessageInfo info = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as TzarGames.MatchFramework.IServerMatchSystem).Authorize(encryptedToken,info);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 2)
			{
				if(target is TzarGames.MatchFramework.IClientMatchSystem == false)
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
						System.Int32 errorCode = stream.ReadInt();
						(target as TzarGames.MatchFramework.IClientMatchSystem).AuthError(errorCode);
						return true;
					}
					#endif
				}
			}
			return false;
		}
	}
}
