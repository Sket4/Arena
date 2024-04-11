using TzarGames.MultiplayerKit;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Arena
{
    [System.Serializable]
    public struct PlayerInputCommand : System.IComparable<PlayerInputCommand>
    {
        public int Index;
        public float DeltaTime;
        public float Horizontal;
        public float Vertical;
        public byte Jump;
        public float3 Position;
        public short AbilityID;
        public NetworkID TargetNetID;
        public float3 ViewDir;
        public float3 TargetPosition;

        public const float MaxPositionError = 0.02f;

        public int CompareTo(PlayerInputCommand other)
        {
            if (Index > other.Index) return 1;
            else if (Index < other.Index) return -1;
            return 0;
        }
    }

    [System.Serializable]
    public struct PlayerInputClientData
    {
        public PlayerInputCommand[] Commands;
        const int HeaderSize = sizeof(ushort);
        static readonly int CommandSize = UnsafeUtility.SizeOf<PlayerInputCommand>();
        public const int MaxCommandsToSend = 5;

        public byte[] ToByteArray()
        {
            if(Commands.Length > MaxCommandsToSend)
            {
                throw new System.Exception($"MaxCommandsCount {MaxCommandsToSend} but trying to send {Commands.Length}");
            }
            var dataSize = HeaderSize + Commands.Length * CommandSize;

            using(var writeStream = new WriteStream(dataSize, Unity.Collections.Allocator.Temp))
            {
                writeStream.Write((ushort)Commands.Length);
                foreach(var command in Commands)
                {
                    writeStream.WriteStruct(command);
                }
                return writeStream.ToByteArray();
            }
        }

#if UNITY_SERVER || UNITY_EDITOR
        public static bool FromByteArray(byte[] bytes, out PlayerInputClientData playerInputData)
        {
            if(bytes.Length < HeaderSize)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError("Invalid data size (no header)");
#endif
                playerInputData = default;
                return false;
            }

            using(var data = new Unity.Collections.NativeArray<byte>(bytes.Length, Unity.Collections.Allocator.Temp))
            {
                data.CopyFrom(bytes);
                var streamReader = new Unity.Collections.DataStreamReader(data);
                var stream = new ReadStream(ref streamReader);

                var commandsCount = stream.ReadUShort();

                if (commandsCount > MaxCommandsToSend)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError("Invalid data (max commands)");
#endif
                    playerInputData = default;
                    return false;
                }


                if (stream.CanReadBytes(commandsCount * CommandSize) == false)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError("Invalid data size (no bytes to read commands)");
#endif
                    playerInputData = default;
                    return false;
                }

                playerInputData = new PlayerInputClientData
                {
                    Commands = new PlayerInputCommand[commandsCount]
                };

                for(int i=0; i<commandsCount; i++)
                {
                    playerInputData.Commands[i] = stream.ReadStruct<PlayerInputCommand>();
                }
                return true;
            }
        }
#endif
    }
}
