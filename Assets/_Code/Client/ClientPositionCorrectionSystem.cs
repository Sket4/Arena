using Unity.CharacterController;
using System.Collections.Generic;
using TzarGames.GameCore;
using TzarGames.GameCore.CharacterController;
using TzarGames.MultiplayerKit;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using NetworkPlayer = TzarGames.MultiplayerKit.NetworkPlayer;

namespace Arena.Client
{
    public struct PositionCorrectionTag : IComponentData
    {
    }

    struct PositionCorrectionInfo : System.IComparable<PositionCorrectionInfo>
    {
        public int Index;
        public CharacterContollerStateData CharacterControllerStateData;

        public int CompareTo(PositionCorrectionInfo other)
        {
            if (Index > other.Index) return 1;
            else if (Index < other.Index) return -1;
            return 0;
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup), OrderFirst = true)]
    [UpdateBefore(typeof(CharacterControllerPhysicsUpdateSystem))]
    public partial class ClientPositionCorrectionSystem : GameSystemBase, IServerCorrectionSystem, IRpcProcessor
    {
        public NetworkIdentity NetIdentity { get; set; }
        
        private TimeSystem timeSystem;
        List<PositionCorrectionInfo> corrections = new List<PositionCorrectionInfo>();
        ClientPositionCorrectionJob correctionJob;

        public void CorrectPositionOnClient(int inputCommandIndex, CharacterContollerStateData stateData)
        {
// #if UNITY_EDITOR
//             UnityEngine.Debug.Log($"Коррекция позиции для команды {inputCommandIndex}");
// #endif
            corrections.Add(new PositionCorrectionInfo
            {
                Index = inputCommandIndex,
                CharacterControllerStateData = stateData,
            });
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            timeSystem = World.GetOrCreateSystemManaged<TimeSystem>();

            correctionJob = new ClientPositionCorrectionJob
            {
                UpdateContext = new CharacterControllerUpdateContext(),
                BaseContext = new KinematicCharacterUpdateContext(),
                ClientCommandBuffers = GetBufferLookup<ClientPlayerInputCommand>(),
                NetworkPlayerLookup = GetComponentLookup<NetworkPlayer>(true),
            };

            correctionJob.UpdateContext.OnSystemCreate(ref CheckedStateRef);
            correctionJob.BaseContext.OnSystemCreate(ref CheckedStateRef);
        }

        protected override void OnSystemUpdate()
        {
            Entities
                .ForEach((
                    DynamicBuffer<ClientPlayerInputCommand> playerInputCommands, 
                    in ControlledCharacter character,
                    in NetworkPlayer player) =>
                {
                    if (player.ItsMe == false)
                    {
                        return;
                    }

                    if (HasComponent<DistanceMove>(character.Entity) == false)
                    {
                        return;
                    }

                    if (playerInputCommands.Length == 0)
                    {
                        return;
                    }

                    var latestCommand = playerInputCommands[playerInputCommands.Length - 1];

                    if (latestCommand.IsSent)
                    {
                        return;
                    }
                    
                    latestCommand.DistanceMove = GetComponent<DistanceMove>(character.Entity); 
                    latestCommand.Collider = GetComponent<PhysicsCollider>(character.Entity);
                    latestCommand.Speed = GetComponent<Speed>(character.Entity).Value;
                    latestCommand.Inputs = GetComponent<CharacterInputs>(character.Entity);
                    
                    playerInputCommands[playerInputCommands.Length - 1] = latestCommand;

                }).Run();
            
            if(corrections.Count == 0)
            {
                return;
            }

            var currentTime = timeSystem.GameTime;

            // создаем событие коррекции
            var commands = CreateEntityCommandBufferParallel();
            var correctionEventEntity = commands.CreateEntity(0);
            commands.AddComponent(0, correctionEventEntity, new PositionCorrectionTag());
            commands.AddComponent(0, correctionEventEntity, new EventTag());

            // применяем корректировку
            corrections.Sort();

            correctionJob.CurrentTime = currentTime;
            correctionJob.LatestCorrection = corrections[corrections.Count-1];
            correctionJob.CorrectState = correctionJob.LatestCorrection.CharacterControllerStateData;
#if UNITY_EDITOR
            Debug.Log($"Коррекция команды с номером {correctionJob.LatestCorrection.Index} в позицию {correctionJob.CorrectState.Position}, самая ранняя коррекция - {corrections[0].Index}");//, dist isMoving {correctState.DistanceMove.IsMoving}, dist accumDist {correctState.DistanceMove.AccumulatedDistance}");
#endif

            correctionJob.ClientCommandBuffers.Update(this);
            correctionJob.NetworkPlayerLookup.Update(this);
            correctionJob.UpdateContext.OnSystemUpdate(ref CheckedStateRef);
            correctionJob.BaseContext.OnSystemUpdate(ref CheckedStateRef, new Unity.Core.TimeData(timeSystem.GameTime, timeSystem.TimeDelta), SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            correctionJob.Run();

            corrections.Clear();
        }
    }

    [BurstCompile]
    [WithAll(typeof(DistanceMove))]
    partial struct ClientPositionCorrectionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<NetworkPlayer> NetworkPlayerLookup;
        public BufferLookup<ClientPlayerInputCommand> ClientCommandBuffers;

        public CharacterContollerStateData CorrectState;
        public PositionCorrectionInfo LatestCorrection;
        public CharacterControllerUpdateContext UpdateContext;
        public KinematicCharacterUpdateContext BaseContext;
        public double CurrentTime;

        public void Execute(CharacterControllerAspect characterAspect, ref PlayerSmoothCorrection smoothCorrection, in PlayerController playerController)
        {
            var player = NetworkPlayerLookup[playerController.Value];

            if (player.ItsMe == false)
            {
                return;
            }
            var inputCommands = ClientCommandBuffers[playerController.Value];
            ref var distanceMove = ref characterAspect.OptinalDistanceMove.ValueRW;

            if (inputCommands.Length == 0)
            {
                CorrectState.Apply(
                    ref characterAspect.CharacterAspect.LocalTransform.ValueRW,
                    ref characterAspect.CharacterAspect.CharacterBody.ValueRW,
                    ref distanceMove
                    );
                return;
            }

            bool commandFound = false;
            int correctedCommandArrayIndex = -1;
            bool isCorrectionPositionEqual = false;

            for (int i = 0; i < inputCommands.Length; i++)
            {
                var clientCommand = inputCommands[i];

                var command = clientCommand.Command;
                if (command.Index == LatestCorrection.Index)
                {
                    commandFound = true;
                    correctedCommandArrayIndex = i;

                    var positionDiff =
                        math.length(command.Position - LatestCorrection.CharacterControllerStateData.Position);
                    if (positionDiff < math.EPSILON)
                    {
                        Debug.Log($"Correction position equal: diff {positionDiff}");
                        isCorrectionPositionEqual = true;
                    }

                    //UnityEngine.Debug.Log($"Command found {command.Index}, delta: {command.DeltaTime}, input: {command.Horizontal}, {command.Vertical}, pos: {command.Position}");
                    break;
                }
            }

            if (isCorrectionPositionEqual)
            {
                return;
            }

            bool isPreviousCommand = LatestCorrection.Index < inputCommands[0].Command.Index;

            if (commandFound == false && isPreviousCommand == false)
            {
                return;
            }

            if (isPreviousCommand)
            {
                correctedCommandArrayIndex = -1;
            }

            var prevUncorrectedPosition = characterAspect.CharacterAspect.LocalTransform.ValueRO.Position;
            bool isCorrectingDistanceMove = CorrectState.DistanceMove.IsMoving;

            CorrectState.Apply(
                ref characterAspect.CharacterAspect.LocalTransform.ValueRW,
                ref characterAspect.CharacterAspect.CharacterBody.ValueRW,
                ref distanceMove
                );

            BaseContext.EnsureCreationOfTmpCollections();

            for (int i = 0; i < inputCommands.Length; i++)
            {
                var clientCommand = inputCommands[i];

                if (clientCommand.IsSent == false)
                {
                    // это еще свежая и не выполненная команда, поэтому не корректируем ее позицию
                    if (isCorrectingDistanceMove == false)
                    {
                        distanceMove = clientCommand.DistanceMove;
                    }
                    continue;
                }

                var command = clientCommand.Command;
                if (i <= correctedCommandArrayIndex)
                {
                    continue;
                }

                // применяем последующие команды еще раз после коррекции
                characterAspect.CharacterControl.ValueRW = clientCommand.Inputs;
                CharacterControllerPhysicsUpdateSystem.ClampMoveVectorLength(ref characterAspect.CharacterControl.ValueRW.MoveVector);
                BaseContext.Time = new Unity.Core.TimeData(BaseContext.Time.ElapsedTime, command.DeltaTime);
                characterAspect.CharacterComponent.ValueRW.GroundMaxSpeed = clientCommand.Speed;
                characterAspect.CharacterAspect.PhysicsCollider.ValueRW = clientCommand.Collider;

                var prevPosition = characterAspect.CharacterAspect.LocalTransform.ValueRO.Position;

                // Update character
                CharacterControllerPhysicsUpdateSystem.PreprocessDistanceMove(ref characterAspect.CharacterComponent.ValueRW.GroundMaxSpeed, in characterAspect.OptinalDistanceMove.ValueRO, clientCommand.Speed, command.DeltaTime);

                characterAspect.PhysicsUpdate(ref UpdateContext, ref BaseContext);

                //Debug.Log($"Cor. step for cmd {command.Index}, from {prevPosition} to {processor.Translation}, relvel: {processor.CharacterBody.RelativeVelocity}");

                if (isCorrectingDistanceMove == false)
                {
                    distanceMove = clientCommand.DistanceMove;
                }

                // DEBUG
                // if (distanceMove.IsMoving)
                // {
                //     Debug.DrawLine(prevPosition, processor.Translation, Color.red, 5);
                //     Debug.DrawRay(processor.Translation, (math.up() + math.normalize(processor.Translation - prevPosition)) * 0.1f, Color.yellow, 5);
                // }

                CharacterControllerPhysicsUpdateSystem.PostProcessDistanceMove(
                    ref distanceMove, 
                    characterAspect.CharacterAspect.LocalTransform.ValueRO.Position,
                    prevPosition
                    );

                if (isCorrectingDistanceMove)
                {
                    if (distanceMove.IsMoving == false)
                    {
                        isCorrectingDistanceMove = false;
                    }
                }

                clientCommand.Command.Position = characterAspect.CharacterAspect.LocalTransform.ValueRO.Position;
                inputCommands[i] = clientCommand;
            }

            // для сглаженной коррекции позиции модели
            var diff = characterAspect.CharacterAspect.LocalTransform.ValueRO.Position - prevUncorrectedPosition;

            if (math.lengthsq(diff) > 0.001f)
            {
                if (smoothCorrection.ShouldCorrect == false)
                {
                    smoothCorrection.ShouldCorrect = true;
                    smoothCorrection.CorrectionStartTime = CurrentTime;
                }

                //Debug.Log($"smooth correct start, disp {smoothCorrection.LocalDisplacement}, old pos {prevUncorrectedPosition}, corrected pos {translation.Value}");    
            }
        }
    }
}
