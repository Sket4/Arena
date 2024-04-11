using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.CharacterController;
using UnityEngine;

namespace TzarGames.GameCore.CharacterController
{
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct CharacterControllerPhysicsUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private CharacterControllerUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;
        
        public static void PreprocessDistanceMove(ref float groundMexSpeed, in DistanceMove distMove, in float speed, in float deltaTime)
        {
            if (distMove.IsMoving)
            {
                var moveDelta = deltaTime * speed;

                if (moveDelta > math.EPSILON)
                {
                    var remainingDistance = math.clamp(distMove.MaxDistance - distMove.AccumulatedDistance, 0, moveDelta);

                    if (remainingDistance < moveDelta)
                    {
                        var brakingFactor = remainingDistance / moveDelta;
                        groundMexSpeed *= brakingFactor;
                    }
                }
            }
        }

        public static void PostProcessDistanceMove(ref DistanceMove move, in float3 position, in float3 prevPosition)
        {
            if (move.IsMoving == false)
            {
                return;
            }
            var delta = math.distance(position, prevPosition);
            move.AccumulatedDistance += delta;

            if (move.AccumulatedDistance >= move.MaxDistance - 0.001f)
            {
                move.IsMoving = false;
            }
        }

        public static void ClampMoveVectorLength(ref float3 moveVector)
        {
            var moveVectorLength = math.length(moveVector);

            if (moveVectorLength > 0)
            {
                var inv_moveVectorLength = 1.0f / moveVectorLength;
                moveVector = moveVector * inv_moveVectorLength * math.clamp(moveVectorLength, 0, 1.0f);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    CharacterControllerComponent,
                    CharacterInputs>()
                .Build(ref state);

            _context = new CharacterControllerUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _context.OnSystemUpdate(ref state);
            
            _baseContext.OnSystemUpdate(ref state, state.WorldUnmanaged.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            CharacterControllerPhysicsUpdateJob job = new CharacterControllerPhysicsUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterControllerPhysicsUpdateJob : IJobEntity
        {
            public CharacterControllerUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(CharacterControllerAspect characterAspect, in Speed speed)
            {
                if (characterAspect.OptionalDeltaTime.IsValid)
                {
                    BaseContext.Time = new Unity.Core.TimeData(BaseContext.Time.ElapsedTime, characterAspect.OptionalDeltaTime.ValueRO.Value);
                }

                if (BaseContext.Time.DeltaTime < 0.001f)
                {
                    return;
                }
                
                //Debug.Log($"delta: {BaseContext.Time.DeltaTime}, elapsed: {BaseContext.Time.ElapsedTime}");

                BaseContext.EnsureCreationOfTmpCollections();

                // clamp input length
                ClampMoveVectorLength(ref characterAspect.CharacterControl.ValueRW.MoveVector);
                
                var prevPosition = characterAspect.CharacterAspect.LocalTransform.ValueRO.Position;

                characterAspect.CharacterComponent.ValueRW.GroundMaxSpeed = speed.Value;
                
                // торможение для достижения точной дистанции
                if(characterAspect.OptinalDistanceMove.IsValid)
                {
                    PreprocessDistanceMove(ref characterAspect.CharacterComponent.ValueRW.GroundMaxSpeed, characterAspect.OptinalDistanceMove.ValueRO, speed.Value, BaseContext.Time.DeltaTime);
                }

                characterAspect.PhysicsUpdate(ref Context, ref BaseContext);

                // if (math.length(characterAspect.CharacterControl.ValueRO.MoveVector) > 0.0f)
                // {
                //     UnityEngine.Debug.Log($"{characterAspect.CharacterAspect.Entity.Index} delta {BaseContext.Time.DeltaTime} Move vect {characterAspect.CharacterControl.ValueRO.MoveVector}, prevpos: {prevPosition}, currpos: {characterAspect.CharacterAspect.LocalTransform.ValueRO.Position}");
                // }
                
                if(characterAspect.OptinalDistanceMove.IsValid)
                {
                    PostProcessDistanceMove(
                        ref characterAspect.OptinalDistanceMove.ValueRW, 
                        in characterAspect.CharacterAspect.LocalTransform.ValueRO.Position, 
                        in prevPosition);
                }

                if (characterAspect.OptinalVelocity.IsValid)
                {
                    ref var velocity = ref characterAspect.OptinalVelocity.ValueRW;
                    velocity.Value = characterAspect.CharacterAspect.CharacterBody.ValueRO.RelativeVelocity;
                    velocity.CachedMagnitude = math.length(velocity.Value);
                }
            }
        }
    }

    /// <summary>
    /// for rotation and parent rotation
    /// </summary>
    [UpdateInGroup(typeof(KinematicCharacterVariableUpdateGroup))]
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct CharacterControllerVariableUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private CharacterControllerUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    CharacterControllerComponent,
                    CharacterInputs>()
                .Build(ref state);

            _context = new CharacterControllerUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _context.OnSystemUpdate(ref state);
            _baseContext.OnSystemUpdate(ref state, state.WorldUnmanaged.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            CharacterControllerVariableUpdateJob job = new CharacterControllerVariableUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterControllerVariableUpdateJob : IJobEntity
        {
            public CharacterControllerUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(CharacterControllerAspect characterAspect)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                characterAspect.VariableUpdate(ref Context, ref BaseContext);
            }
        }
    }
}
