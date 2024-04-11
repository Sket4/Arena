using Unity.Entities;
using Unity.CharacterController;
using Unity.Mathematics;
using Unity.Physics;

namespace TzarGames.GameCore.CharacterController
{
    public struct CharacterControllerUpdateContext
    {
        // Here, you may add additional global data for your character updates, such as ComponentLookups, Singletons, NativeCollections, etc...
        // The data you add here will be accessible in your character updates and all of your character "callbacks".

        public void OnSystemCreate(ref SystemState state)
        { }

        public void OnSystemUpdate(ref SystemState state)
        { }
    }

    public readonly partial struct CharacterControllerAspect : IAspect, IKinematicCharacterProcessor<CharacterControllerUpdateContext>
    {
        public readonly KinematicCharacterAspect CharacterAspect;
        public readonly RefRW<CharacterControllerComponent> CharacterComponent;
        public readonly RefRW<CharacterInputs> CharacterControl;

        [Optional]
        public readonly RefRO<DeltaTime> OptionalDeltaTime;
        [Optional]
        public readonly RefRW<DistanceMove> OptinalDistanceMove;
        [Optional]
        public readonly RefRW<Velocity> OptinalVelocity;

        public void PhysicsUpdate(ref CharacterControllerUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
        {
            ref CharacterControllerComponent characterComponent = ref CharacterComponent.ValueRW;
            ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
            ref float3 characterPosition = ref CharacterAspect.LocalTransform.ValueRW.Position;

            // First phase of default character update
            CharacterAspect.Update_Initialize(in this, ref context, ref baseContext, ref characterBody, baseContext.Time.DeltaTime);
            CharacterAspect.Update_ParentMovement(in this, ref context, ref baseContext, ref characterBody, ref characterPosition, characterBody.WasGroundedBeforeCharacterUpdate);
            CharacterAspect.Update_Grounding(in this, ref context, ref baseContext, ref characterBody, ref characterPosition);

            // Update desired character velocity after grounding was detected, but before doing additional processing that depends on velocity
            HandleVelocityControl(ref context, ref baseContext);

            // Second phase of default character update
            CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in this, ref context, ref baseContext, ref characterBody, in characterComponent.StepAndSlopeHandling);
            CharacterAspect.Update_GroundPushing(in this, ref context, ref baseContext, characterComponent.Gravity);
            CharacterAspect.Update_MovementAndDecollisions(in this, ref context, ref baseContext, ref characterBody, ref characterPosition);
            CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterBody);
            CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterBody);
            CharacterAspect.Update_ProcessStatefulCharacterHits();
        }

        private void HandleVelocityControl(ref CharacterControllerUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
        {
            float deltaTime = baseContext.Time.DeltaTime;
            ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
            ref CharacterControllerComponent characterComponent = ref CharacterComponent.ValueRW;
            ref CharacterInputs characterControl = ref CharacterControl.ValueRW;

            if (characterBody.IsGrounded)
            {
                // Move on ground
                float3 targetVelocity = characterControl.MoveVector * characterComponent.GroundMaxSpeed;
                
                var groundMoveSharpness = OptinalDistanceMove.IsValid && OptinalDistanceMove.ValueRO.IsMoving
                    ? 9999f
                    : characterComponent.GroundedMovementSharpness;
                
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity, targetVelocity, groundMoveSharpness, deltaTime, characterBody.GroundingUp, characterBody.GroundHit.Normal);

                // Jump
                if (characterControl.JumpRequested)
                {
                    CharacterControlUtilities.StandardJump(ref characterBody, characterBody.GroundingUp * characterComponent.JumpSpeed, true, characterBody.GroundingUp);
                }
            }
            else
            {
                // Move in air
                float3 airAcceleration = characterControl.MoveVector * characterComponent.AirAcceleration;
                if (math.lengthsq(airAcceleration) > 0f)
                {
                    float3 tmpVelocity = characterBody.RelativeVelocity;
                    CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration, characterComponent.AirMaxSpeed, characterBody.GroundingUp, deltaTime, false);

                    // Cancel air acceleration from input if we would hit a non-grounded surface (prevents air-climbing slopes at high air accelerations)
                    if (characterComponent.PreventAirAccelerationAgainstUngroundedHits && CharacterAspect.MovementWouldHitNonGroundedObstruction(in this, ref context, ref baseContext, characterBody.RelativeVelocity * deltaTime, out ColliderCastHit hit))
                    {
                        characterBody.RelativeVelocity = tmpVelocity;
                    }
                }

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity, characterComponent.Gravity, deltaTime);

                // Drag
                CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime, characterComponent.AirDrag);
            }
        }

        public void VariableUpdate(ref CharacterControllerUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
        {
            ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
            ref CharacterControllerComponent characterComponent = ref CharacterComponent.ValueRW;
            ref CharacterInputs characterControl = ref CharacterControl.ValueRW;
            ref quaternion characterRotation = ref CharacterAspect.LocalTransform.ValueRW.Rotation;

            // Add rotation from parent body to the character rotation
            // (this is for allowing a rotating moving platform to rotate your character as well, and handle interpolation properly)
            KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(ref characterRotation, characterBody.RotationFromParent, baseContext.Time.DeltaTime, characterBody.LastPhysicsUpdateDeltaTime);

            // Rotate towards move direction
            if (math.lengthsq(characterControl.MoveVector) > 0f)
            {
                CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref characterRotation, baseContext.Time.DeltaTime, math.normalizesafe(characterControl.MoveVector), MathUtilities.GetUpFromRotation(characterRotation), characterComponent.RotationSharpness);
            }
        }

        #region Character Processor Callbacks
        public void UpdateGroundingUp(
            ref CharacterControllerUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext)
        {
            ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;

            CharacterAspect.Default_UpdateGroundingUp(ref characterBody);
        }

        public bool CanCollideWithHit(
            ref CharacterControllerUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit)
        {
            return PhysicsUtilities.IsCollidable(hit.Material);
        }

        public bool IsGroundedOnHit(
            ref CharacterControllerUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit,
            int groundingEvaluationType)
        {
            CharacterControllerComponent characterComponent = CharacterComponent.ValueRO;

            return CharacterAspect.Default_IsGroundedOnHit(
                in this,
                ref context,
                ref baseContext,
                in hit,
                in characterComponent.StepAndSlopeHandling,
                groundingEvaluationType);
        }

        public void OnMovementHit(
                ref CharacterControllerUpdateContext context,
                ref KinematicCharacterUpdateContext baseContext,
                ref KinematicCharacterHit hit,
                ref float3 remainingMovementDirection,
                ref float remainingMovementLength,
                float3 originalVelocityDirection,
                float hitDistance)
        {
            ref KinematicCharacterBody characterBody = ref CharacterAspect.CharacterBody.ValueRW;
            ref float3 characterPosition = ref CharacterAspect.LocalTransform.ValueRW.Position;
            CharacterControllerComponent characterComponent = CharacterComponent.ValueRO;

            CharacterAspect.Default_OnMovementHit(
                in this,
                ref context,
                ref baseContext,
                ref characterBody,
                ref characterPosition,
                ref hit,
                ref remainingMovementDirection,
                ref remainingMovementLength,
                originalVelocityDirection,
                hitDistance,
                characterComponent.StepAndSlopeHandling.StepHandling,
                characterComponent.StepAndSlopeHandling.MaxStepHeight,
                characterComponent.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck
                );
        }

        public void OverrideDynamicHitMasses(
            ref CharacterControllerUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref PhysicsMass characterMass,
            ref PhysicsMass otherMass,
            BasicHit hit)
        {
        }

        public void ProjectVelocityOnHits(
            ref CharacterControllerUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref float3 velocity,
            ref bool characterIsGrounded,
            ref BasicHit characterGroundHit,
            in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits,
            float3 originalVelocityDirection)
        {
            CharacterControllerComponent characterComponent = CharacterComponent.ValueRO;

            CharacterAspect.Default_ProjectVelocityOnHits(
                ref velocity,
                ref characterIsGrounded,
                ref characterGroundHit,
                in velocityProjectionHits,
                originalVelocityDirection,
                characterComponent.StepAndSlopeHandling.ConstrainVelocityToGroundPlane);
        }
        #endregion
    }
}
