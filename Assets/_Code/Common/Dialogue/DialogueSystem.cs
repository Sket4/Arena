using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using TzarGames.GameCore.ScriptViz;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Dialogue
{
    public struct DialogueAnswerSignal : IComponentData
    {
        public Entity Player;
        public Entity DialogueEntity;
        public Address CommandAddress;
    }
    
    /// <summary>
    /// должен вызываться после GameCommandBufferSystem, чтобы сразу обработать запросы на диалог и уничтожить их при необходимости, пока они не обработаны другими системами
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DialogueSystem : SystemBase
    {
        private Entity dialogueBlockerEntity;
        
        protected override void OnCreate()
        {
            base.OnCreate();

            dialogueBlockerEntity = EntityManager.CreateEntity();
            EntityManager.SetName(dialogueBlockerEntity, "Dialogue blocker");
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            var commands = new UniversalCommandBuffer(ecb);
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            Entities.ForEach((Entity entity, in DialogueMessage message) =>
            {
                if (message.Player == Entity.Null || message.Companion == Entity.Null)
                {
                    Debug.LogError($"dialogue companion is null, destroying dialogue message {entity.Index}");
                    commands.DestroyEntityWithDefaultKey(entity);
                    return;
                }

                bool player_hasLivingState = SystemAPI.HasComponent<LivingState>(message.Player);
                bool companion_hasLivingState = SystemAPI.HasComponent<LivingState>(message.Companion);
                
                if (player_hasLivingState == false && companion_hasLivingState == false)
                {
                    Debug.LogError($"dialogue companions {message.Player.Index} and {message.Companion.Index} do not have LivingState component, destroying message {entity.Index}");
                    commands.DestroyEntityWithDefaultKey(entity);
                    return;
                }

                if (player_hasLivingState)
                {
                    var livingState = SystemAPI.GetComponent<LivingState>(message.Player);

                    if (livingState.IsAlive == false)
                    {
                        Debug.LogWarning($"dialogue player {message.Player.Index} is dead, destroying message {entity.Index}");
                        commands.DestroyEntityWithDefaultKey(entity);
                        return;
                    }
                }

                if (companion_hasLivingState)
                {
                    var livingState = SystemAPI.GetComponent<LivingState>(message.Companion);

                    if (livingState.IsAlive == false)
                    {
                        Debug.LogWarning($"dialogue companion {message.Companion.Index} is dead, destroying message {entity.Index}");
                        commands.DestroyEntityWithDefaultKey(entity);
                        return;
                    }
                }

                var currentState = SystemAPI.GetComponent<DialogueState>(message.Player);

                if (currentState.Companion != Entity.Null && currentState.Companion != message.Companion)
                {
                    Debug.LogError($"failed to start dialog with companion {message.Companion.Index} - already has companion {currentState.Companion.Index}");
                    commands.DestroyEntityWithDefaultKey(entity);
                    return;
                }

                if (currentState.DialogueEntity != Entity.Null && currentState.DialogueEntity != message.DialogueEntity)
                {
                    Debug.LogError($"failed to start dialog by {message.DialogueEntity.Index} - already has dialogue {currentState.DialogueEntity.Index}");
                    commands.DestroyEntityWithDefaultKey(entity);
                    return;
                }
                
                // DIALOGUE IS OK
                SystemAPI.SetComponent(message.Player, new DialogueState
                {
                    Companion = message.Companion,
                    DialogueEntity = message.DialogueEntity,
                });
                commands.SetComponentEnabled<DialogueUpdateState>(0, message.Player, true);
                commands.SetComponent(0, message.Player, new DialogueUpdateState
                {
                    StartPosition = SystemAPI.GetComponent<LocalTransform>(message.Player).Position
                });

            }).Run();
            
            Entities.ForEach((Entity entity, int entityInQueryIndex, in DialogueAnswerSignal signal) =>
            {
                commands.DestroyEntity(entityInQueryIndex, entity);
                
                SystemAPI.SetComponent(signal.Player, new DialogueState
                {
                    Companion = Entity.Null,
                    DialogueEntity = Entity.Null
                });
                commands.SetComponentEnabled<DialogueUpdateState>(0, signal.Player, false);

                var aspect = SystemAPI.GetAspect<ScriptVizAspect>(signal.DialogueEntity);
                            
                var codeBytes = SystemAPI.GetBuffer<CodeDataByte>(aspect.CodeInfo.ValueRO.CodeDataEntity);
                var constEntityVarData = SystemAPI.GetBuffer<ConstantEntityVariableData>(aspect.CodeInfo.ValueRO.CodeDataEntity);

                using (var contextHandle = new ContextDisposeHandle(codeBytes, constEntityVarData, ref aspect, ref commands, entityInQueryIndex, deltaTime))
                {
                    if (signal.CommandAddress.IsInvalid)
                    {       
                        Debug.LogError($"invalid command address {signal.CommandAddress.Value}");
                        return;
                    }
                    contextHandle.Execute(signal.CommandAddress);  
                }

            }).Run();
            
            Entities
                .ForEach((Entity entity, in DialogueUpdateState state, in LocalTransform transform) =>
            {
                if (math.distancesq(transform.Position, state.StartPosition) < 0.1f)
                {
                    return;
                }
                Debug.LogWarning("stop dialogue due to player displacement");
                
                commands.SetComponent(0, entity, new DialogueState
                {
                    Companion = Entity.Null,
                    DialogueEntity = Entity.Null
                });
                commands.SetComponentEnabled<DialogueUpdateState>(0, entity, false);
                
            }).Run();

            var blocker = dialogueBlockerEntity;
            
            Entities
                .WithChangeFilter<DialogueState>()
                .ForEach((DynamicBuffer<SpeedModificator> speedMods, DynamicBuffer<CharacterAbilityBlocker> abilityBlockers, in DialogueState state) =>
            {
                if (state.DialogueEntity != Entity.Null)
                {
                    // block movement
                    if (IOwnedModificatorExtensions.IsAddedToBuffer(blocker, speedMods) == false)
                    {
                        speedMods.Add(new SpeedModificator
                        {
                            Owner = blocker,
                            Value = new CharacteristicModificator
                            {
                                Value = 0,
                                Operator = ModificatorOperators.MULTIPLY_ACTUAL
                            }
                        });
                    }
                    
                    // block abilities
                    if (CharacterAbilityBlocker.ExistInBuffer(blocker, abilityBlockers) == false)
                    {
                        abilityBlockers.Add(new CharacterAbilityBlocker
                        {
                            Entity = blocker,
                            ForceStopCurrentAbility = true
                        });
                    }
                }
                else
                {
                    // unblock movement
                    IOwnedModificatorExtensions.RemoveModificatorWithOwner(blocker, speedMods);
                    // unblock abilities
                    CharacterAbilityBlocker.RemoveFromBuffer(blocker, abilityBlockers);
                }
                
            }).Run();
            
            Dependency.Complete();
            ecb.Playback(EntityManager);
        }
    }
}
