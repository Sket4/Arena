using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.CharacterController;

namespace Arena
{
	public interface IInputSyncSystem
	{
		[RemoteCall(ChannelType.Unreliable, canBeCalledFromClient: true, canBeCalledByNonOwner: true)]
		void SendInputToServer(byte[] inputData, NetMessageInfo messageInfo);
	}

	public struct CharacterContollerStateData
	{
		public bool IsGrounded;
		public float3 RelativeVelocity;
		public float3 Position;
		public quaternion Rotation;

		public DistanceMove DistanceMove;
		//public bool IsJumping;

		public CharacterContollerStateData(bool isGrounded, float3 relativeVelocity, float3 position, quaternion rotation, DistanceMove distanceMove)
		{
			IsGrounded = isGrounded;
			RelativeVelocity = relativeVelocity;
			Rotation = rotation;
			Position = position;
			DistanceMove = distanceMove;
		}

        public void Apply(ref LocalTransform transform, ref KinematicCharacterBody characterBody, ref DistanceMove distanceMove)
        {
			transform.Position = Position;
			transform.Rotation = Rotation;
			characterBody.RelativeVelocity = RelativeVelocity;
			characterBody.IsGrounded = IsGrounded;
			distanceMove = DistanceMove;	
        }
    }

	public interface IServerCorrectionSystem
	{
		[RemoteCall(ChannelType.Unreliable, canBeCalledFromClient: false)]
		void CorrectPositionOnClient(int inputCommandIndex, CharacterContollerStateData controllerInternalData);
	}
}
