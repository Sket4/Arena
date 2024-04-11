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
			if(rpcHandlerType == typeof(TzarGames.GameCore.Tests.TestSpawnSystem))
			{
				info = new RemoteCallInfo(1,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "Spawn": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			info = default;
			return false;
		}

		public bool Call(NetworkPlayer owner, Entity senderEntity, NetworkPlayer sender, byte handlerCode, byte rpcCode, INetworkObject target, bool isServer, ref DataStreamReader reader, EntityCommandBuffer commands)
		{
			if(handlerCode == 1)
			{
				if(target.GetType() != typeof(TzarGames.GameCore.Tests.TestSpawnSystem))
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
						System.Int32 networkPlayerID = stream.ReadInt();
						TzarGames.MultiplayerKit.NetworkID id = stream.ReadStruct<NetworkID>();
						(target as TzarGames.GameCore.Tests.TestSpawnSystem).Spawn(networkPlayerID,id);
						return true;
					}
					#endif
				}
			}
			return false;
		}
	}
}
