using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.CharacterController;

namespace TzarGames.GameCore.CharacterController
{
    [Serializable]
    public struct CharacterControllerComponent : IComponentData, IEnableableComponent
    {
        public float RotationSharpness;
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

        public static CharacterControllerComponent GetDefault()
        {
            return new CharacterControllerComponent
            {
                RotationSharpness = 25f,
                GroundMaxSpeed = 10f,
                GroundedMovementSharpness = 15f,
                AirAcceleration = 50f,
                AirMaxSpeed = 10f,
                AirDrag = 0f,
                JumpSpeed = 10f,
                Gravity = math.up() * -30f,
                PreventAirAccelerationAgainstUngroundedHits = true,
                StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault(),
            };
        }
    }
}
