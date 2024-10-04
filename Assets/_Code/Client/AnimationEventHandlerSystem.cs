using Arena.Client.ScriptViz;
using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using TzarGames.GameCore.ScriptViz;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(AnimationSystemGroup))]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class AnimationEventHandlerSystem : GameSystemBase
    {
        private static readonly int FootstepFuncHash = TzarGames.AnimationFramework.Utility.GetStableHashCode("Footstep");
        private static readonly int WeaponSwingFuncHash = TzarGames.AnimationFramework.Utility.GetStableHashCode("WeaponSwing");
        
        protected override void OnSystemUpdate()
        {
            UniversalCommandBuffer commands = CreateUniversalCommandBuffer();
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            Entities
                .WithoutBurst()
                .ForEach((in AnimationEventData animEvent) =>
            {
                var funcName = animEvent.Event.FunctionName.ToString();

                if(funcName == "Footstep")
                {
                    playFootstep(in animEvent, ref commands);
                }
                else if (funcName == "WeaponSwing")
                {
                    playWeaponSwing(in animEvent, ref commands);
                }
                else
                {
                    Entity svEntity;

                    if (SystemAPI.HasBuffer<AnimationEventCommand>(animEvent.SourceEntity))
                    {
                        svEntity = animEvent.SourceEntity;
                    }
                    else if (SystemAPI.HasComponent<Owner>(animEvent.SourceEntity))
                    {
                        var owner = SystemAPI.GetComponent<Owner>(animEvent.SourceEntity);

                        if (SystemAPI.HasBuffer<AnimationEventCommand>(owner.Value))
                        {
                            svEntity = owner.Value;
                        }
                        else
                        {
                            svEntity = Entity.Null;
                        }
                    }
                    else
                    {
                        svEntity = Entity.Null;
                    }
                    
                    if(svEntity != Entity.Null)
                    {
                        var aspect = SystemAPI.GetAspect<ScriptVizAspect>(svEntity);
                        var handle = new ContextDisposeHandle(ref aspect, ref commands, 0, deltaTime);
                        var events = SystemAPI.GetBuffer<AnimationEventCommand>(svEntity);

                        foreach (var command in events)
                        {
                            if (command.EventID == animEvent.Event.IntParameter)
                            {
                                if (command.CommandAddress.IsInvalid)
                                {
                                    continue;
                                }
                                handle.Execute(command.CommandAddress);    
                            }
                        }
                    }
                }
                
            }).Run();
        }

        unsafe void playFootstep(in AnimationEventData animEvent, ref UniversalCommandBuffer commands)
        {
            if(SystemAPI.HasComponent<Owner>(animEvent.SourceEntity) == false)
            {
                Debug.LogError($"no owner component for entity {animEvent.SourceEntity}");
                return;
            }
            var owner = SystemAPI.GetComponent<Owner>(animEvent.SourceEntity);

            if (SystemAPI.HasComponent<DistanceToGround>(owner.Value) == false)
            {
                Debug.LogError($"no distance to ground component for entity {owner.Value}");
                return;
            }

            var distanceToGround = SystemAPI.GetComponent<DistanceToGround>(owner.Value);
            
            if (distanceToGround.CurrentHit.Entity == Entity.Null)
            {
                return;
            }
            
            initCommands(ref commands);

            DynamicBuffer<FootstepSoundGroupElement> footstepSounds;
            var footstepSoundsEntity = SystemAPI.GetSingletonEntity<FootstepSoundsTag>();
            var footstepSharedData = SystemAPI.GetComponent<FootstepSoundsShared>(footstepSoundsEntity);
            bool isInWater = false;
            //WaterState waterState = default;

            if (SystemAPI.HasComponent<WaterState>(owner.Value))
            {
                isInWater = SystemAPI.IsComponentEnabled<WaterState>(owner.Value);
                //waterState = SystemAPI.GetComponent<WaterState>(owner.Value);
            }
            
            Entity targetGroupEntity = Entity.Null;

            if (isInWater)
            {
                targetGroupEntity = footstepSharedData.WaterSoundsEntity;
            }
            else
            {
                if(SystemAPI.HasBuffer<FootstepSoundGroupElement>(animEvent.SourceEntity))
                {
                    footstepSounds = SystemAPI.GetBuffer<FootstepSoundGroupElement>(animEvent.SourceEntity);
                }
                else
                {
                    footstepSounds = SystemAPI.GetBuffer<FootstepSoundGroupElement>(footstepSoundsEntity);
                }
            
                var currentHit = distanceToGround.CurrentHit;

                if (EntityManager.HasComponent<TerrainPhysicsMaterial>(currentHit.Entity))
                {
                    var terrainMat = EntityManager.GetComponentData<TerrainPhysicsMaterial>(currentHit.Entity);
                    var traceResult = terrainMat.WorldPositionToLayer(currentHit.Position);
                    //Debug.Log($"has terrain material {currentHit.Entity}, trace: {traceResult}");
                
                    foreach (var footstepSoundGroup in footstepSounds)
                    {
                        if ((footstepSoundGroup.PhysicsMaterialTags & traceResult) > 0)
                        {
                            targetGroupEntity = footstepSoundGroup.SoundGroupEntity;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var footstepSoundGroup in footstepSounds)
                    {
                        if ((footstepSoundGroup.PhysicsMaterialTags & currentHit.Material.CustomTags) > 0)
                        {
                            targetGroupEntity = footstepSoundGroup.SoundGroupEntity;
                            break;
                        }
                    }
                }
            
                if (targetGroupEntity == Entity.Null)
                {
                    targetGroupEntity = footstepSounds[0].SoundGroupEntity;
                }
            }
            
            
            var groupInstance = commands.Instantiate(0, targetGroupEntity);
            commands.AddComponent(0, groupInstance, new PlaySoundEvent());

            float3 soundPos = default;
            Entity foot = Entity.Null;

            if (SystemAPI.HasComponent<ArmorSetAppearance>(animEvent.SourceEntity))
            {
                var armorSet = SystemAPI.GetComponent<ArmorSetAppearance>(animEvent.SourceEntity);
                
                var strParam = animEvent.Event.StringParameter.ToString();

                if(strParam == "Left")
                {
                    foot = armorSet.LeftFoot;
                }
                else if(strParam == "Right")
                {
                    foot = armorSet.RightFoot;
                }
            }
            else if(SystemAPI.HasComponent<LocalToWorld>(animEvent.SourceEntity))
            {
                foot = animEvent.SourceEntity;
            }

            if (SystemAPI.HasComponent<LocalToWorld>(foot))
            {
                soundPos = SystemAPI.GetComponent<LocalToWorld>(foot).Position;
            }

            //Debug.DrawRay(soundPos, Vector3.up, Color.red, 10);

            commands.AddComponent(0, groupInstance, LocalTransform.FromPosition(soundPos));
        }

        void playWeaponSwing(in AnimationEventData animEvent, ref UniversalCommandBuffer commands)
        {
            initCommands(ref commands);

            var sounds = SystemAPI.GetSingleton<WeaponSounds>();

            var group = sounds.SwordSwingsGroup;

            var groupInstance = commands.Instantiate(0, group);
            commands.AddComponent(0, groupInstance, new PlaySoundEvent());

            float3 soundPos = default;

            if (SystemAPI.HasComponent<Owner>(animEvent.SourceEntity))
            {
                var owner = SystemAPI.GetComponent<Owner>(animEvent.SourceEntity);

                soundPos = SystemAPI.GetComponent<LocalToWorld>(owner.Value).Position;

                if(SystemAPI.HasComponent<AttackVerticalOffset>(owner.Value))
                {
                    var offset = SystemAPI.GetComponent<AttackVerticalOffset>(owner.Value);
                    soundPos += new Unity.Mathematics.float3(0,offset.Value,0);
                }
            }
            else
            {
                soundPos = SystemAPI.GetComponent<LocalToWorld>(animEvent.SourceEntity).Position;
            }

            //UnityEngine.Debug.Log($"swing pos {soundPos.Value}");

            commands.AddComponent(0, groupInstance, LocalTransform.FromPosition(soundPos));
        }

        void initCommands(ref UniversalCommandBuffer commandBuffer)
        {
            if (commandBuffer.IsCreated == false)
            {
                commandBuffer = CreateUniversalCommandBuffer();
            }
        }
    }
}
