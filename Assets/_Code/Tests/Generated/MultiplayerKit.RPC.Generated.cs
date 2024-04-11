using UnityEngine;
using Unity.Networking.Transport;
using System.Reflection;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TzarGames.MultiplayerKit.Generated
{
	class MultiplayerKit_RpcHandler : IRpcHandler
	{
		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod]
		private static void init()
		{
			NetworkRpcSystem.RegisterRpcHandler(new MultiplayerKit_RpcHandler());
		}

		public bool GetRpcCode(System.Type rpcHandlerType, MethodInfo method, out RemoteCallInfo info)
		{
			if(rpcHandlerType == typeof(TzarGames.MultiplayerKit.Tests.RpcTest))
			{
				info = new RemoteCallInfo(4,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "ManyParamsForUnreliable": info.MethodCode = 6; info.Channel = ChannelType.Unreliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "ManyParamsRPC": info.MethodCode = 1; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "Repeating": info.MethodCode = 3; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 5; return true;
					case "Response": info.MethodCode = 11; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "UnmanagedStructRPC": info.MethodCode = 2; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "BigNetChunkRpc": info.MethodCode = 4; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "NetMessageInfoCall": info.MethodCode = 9; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "NoParamForUnreliableSeq": info.MethodCode = 7; info.Channel = ChannelType.UnreliableSequenced; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "NoParamForUnreliable": info.MethodCode = 5; info.Channel = ChannelType.Unreliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "ManyParamsForUnreliableSeq": info.MethodCode = 8; info.Channel = ChannelType.UnreliableSequenced; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "NoParam": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "Callback": info.MethodCode = 10; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			if(rpcHandlerType == typeof(TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour))
			{
				info = new RemoteCallInfo(5,0, ChannelType.Reliable, MessageDeliveryOptions.Default, 1);
				switch(method.Name)
				{
					case "stringRpc": info.MethodCode = 0; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "quatRpc": info.MethodCode = 2; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "vector3Rpc": info.MethodCode = 1; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "bigRpc": info.MethodCode = 4; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
					case "rpc": info.MethodCode = 3; info.Channel = ChannelType.Reliable; info.Options = MessageDeliveryOptions.Default; info.RepeatCount = 1; return true;
				}
			}
			info = default;
			return false;
		}

		public bool Call(NetworkPlayer owner, Entity senderEntity, NetworkPlayer sender, byte handlerCode, byte rpcCode, INetworkObject target, bool isServer, ref DataStreamReader reader, EntityCommandBuffer commands)
		{
			if(handlerCode == 4)
			{
				if(target.GetType() != typeof(TzarGames.MultiplayerKit.Tests.RpcTest))
				{
					return false;
				}
				switch(rpcCode)
				{
					#if UNITY_SERVER || UNITY_EDITOR
					case 6:
					{
						var stream = new ReadStream(ref reader);
						System.String pstr = (System.String)stream.Read(typeof(System.String));
						System.Byte pb = stream.ReadByte();
						System.Int32 pi = stream.ReadInt();
						System.UInt32 pui = stream.ReadUInt();
						System.Int16 ps = stream.ReadShort();
						System.UInt16 pus = stream.ReadUShort();
						System.Single pf = stream.ReadFloat();
						System.Byte[] parr = (System.Byte[])stream.Read(typeof(System.Byte[]));
						TzarGames.MultiplayerKit.Tests.TestStruct pstruct = (TzarGames.MultiplayerKit.Tests.TestStruct)stream.Read(typeof(TzarGames.MultiplayerKit.Tests.TestStruct));
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).ManyParamsForUnreliable(pstr,pb,pi,pui,ps,pus,pf,parr,pstruct);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 1:
					{
						var stream = new ReadStream(ref reader);
						System.String pstr = (System.String)stream.Read(typeof(System.String));
						System.Byte pb = stream.ReadByte();
						System.Int32 pi = stream.ReadInt();
						System.UInt32 pui = stream.ReadUInt();
						System.Int16 ps = stream.ReadShort();
						System.UInt16 pus = stream.ReadUShort();
						System.Single pf = stream.ReadFloat();
						System.Byte[] parr = (System.Byte[])stream.Read(typeof(System.Byte[]));
						TzarGames.MultiplayerKit.Tests.TestStruct pstruct = (TzarGames.MultiplayerKit.Tests.TestStruct)stream.Read(typeof(TzarGames.MultiplayerKit.Tests.TestStruct));
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).ManyParamsRPC(pstr,pb,pi,pui,ps,pus,pf,parr,pstruct);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 3:
					{
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).Repeating();
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 11:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						System.Int32 messageId = stream.ReadInt();
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).Response(messageId);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 2:
					{
						var stream = new ReadStream(ref reader);
						TzarGames.MultiplayerKit.Tests.TestUnmanagedStruct testUnmanagedStruct = stream.ReadStruct<TzarGames.MultiplayerKit.Tests.TestUnmanagedStruct>();
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).UnmanagedStructRPC(testUnmanagedStruct);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 4:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						System.Byte[] data = (System.Byte[])stream.Read(typeof(System.Byte[]));
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).BigNetChunkRpc(data);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 9:
					{
						var stream = new ReadStream(ref reader);
						System.Int32 number = stream.ReadInt();
						TzarGames.MultiplayerKit.NetMessageInfo messageInfo = new NetMessageInfo() { Sender = sender, SenderEntity = senderEntity };
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).NetMessageInfoCall(number,messageInfo);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 7:
					{
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).NoParamForUnreliableSeq();
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 5:
					{
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).NoParamForUnreliable();
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 8:
					{
						var stream = new ReadStream(ref reader);
						System.String pstr = (System.String)stream.Read(typeof(System.String));
						System.Byte pb = stream.ReadByte();
						System.Int32 pi = stream.ReadInt();
						System.UInt32 pui = stream.ReadUInt();
						System.Int16 ps = stream.ReadShort();
						System.UInt16 pus = stream.ReadUShort();
						System.Single pf = stream.ReadFloat();
						System.Byte[] parr = (System.Byte[])stream.Read(typeof(System.Byte[]));
						TzarGames.MultiplayerKit.Tests.TestStruct pstruct = (TzarGames.MultiplayerKit.Tests.TestStruct)stream.Read(typeof(TzarGames.MultiplayerKit.Tests.TestStruct));
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).ManyParamsForUnreliableSeq(pstr,pb,pi,pui,ps,pus,pf,parr,pstruct);
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 0:
					{
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).NoParam();
						return true;
					}
					#endif
					#if UNITY_SERVER || UNITY_EDITOR
					case 10:
					{
						var stream = new ReadStream(ref reader);
						System.Int32 messageId = stream.ReadInt();
						(target as TzarGames.MultiplayerKit.Tests.RpcTest).Callback(messageId);
						return true;
					}
					#endif
				}
			}
			if(handlerCode == 5)
			{
				if(target.GetType() != typeof(TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour))
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
						System.String stringParam = (System.String)stream.Read(typeof(System.String));
						(target as TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour).stringRpc(stringParam);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 2:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						UnityEngine.Quaternion param = stream.ReadQuaternion();
						(target as TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour).quatRpc(param);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 1:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						UnityEngine.Vector3 param = stream.ReadVector3();
						(target as TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour).vector3Rpc(param);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 4:
					{
						if(isServer) return false;
						var stream = new ReadStream(ref reader);
						System.String stringParam = (System.String)stream.Read(typeof(System.String));
						System.Int32 intParam = stream.ReadInt();
						System.UInt32 uintParam = stream.ReadUInt();
						System.Single floatParam = stream.ReadFloat();
						System.Byte[] byteArrayParam = (System.Byte[])stream.Read(typeof(System.Byte[]));
						System.UInt16 ushortParam = stream.ReadUShort();
						System.Int16 shortParam = stream.ReadShort();
						System.Byte byteParam = stream.ReadByte();
						(target as TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour).bigRpc(stringParam,intParam,uintParam,floatParam,byteArrayParam,ushortParam,shortParam,byteParam);
						return true;
					}
					#endif
					#if !UNITY_SERVER
					case 3:
					{
						if(isServer) return false;
						(target as TzarGames.MultiplayerKit.Tests.RpcTesterBehaviour).rpc();
						return true;
					}
					#endif
				}
			}
			return false;
		}
	}
}
