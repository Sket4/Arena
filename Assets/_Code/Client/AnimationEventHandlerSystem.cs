using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(AnimationSystemGroup))]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    public partial class AnimationEventHandlerSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            UniversalCommandBuffer commands = default;
            var footstepSoundGroupBuffers = GetBufferLookup<FootstepSoundGroupElement>(true);
            
            Entities
                .WithoutBurst()
                .ForEach((in AnimationEventData animEvent) =>
            {
                var funcName = animEvent.Event.FunctionName.ToString();

                switch(funcName)
                {
                    case "Footstep":
                        playFootstep(in animEvent, ref commands, ref footstepSoundGroupBuffers);
                        break;

                    case "WeaponSwing":
                        playWeaponSwing(in animEvent, ref commands);
                        break;
                }

                
            }).Run();
        }

        void playFootstep(in AnimationEventData animEvent, ref UniversalCommandBuffer commands, ref BufferLookup<FootstepSoundGroupElement> footstepSoundGroupBuffers)
        {
            initCommands(ref commands);

            Entity group;
            DynamicBuffer<FootstepSoundGroupElement> footstepSounds;

            if(footstepSoundGroupBuffers.TryGetBuffer(animEvent.SourceEntity, out footstepSounds))
            {
                group = footstepSounds[0].SoundGroupEntity;
            }
            else
            {
                var footstepSoundsEntity = SystemAPI.GetSingletonEntity<FootstepSoundsTag>();
                footstepSounds = SystemAPI.GetBuffer<FootstepSoundGroupElement>(footstepSoundsEntity);
                group = footstepSounds[0].SoundGroupEntity;
            }

            var groupInstance = commands.Instantiate(0, group);
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

            //UnityEngine.Debug.Log($"footstep pos {soundPos.Value}");

            commands.SetComponent(0, groupInstance, LocalTransform.FromPosition(soundPos));
        }

        void playWeaponSwing(in AnimationEventData animEvent, ref UniversalCommandBuffer commands)
        {
            initCommands(ref commands);

            var sounds = SystemAPI.GetSingleton<WeaponSounds>();

            var group = sounds.SwordSwingsGroup;

            var groupInstance = commands.Instantiate(0, group);
            commands.AddComponent(0, groupInstance, new PlaySoundEvent());

            float3 soundPos = default;

            if (SystemAPI.HasComponent<ArmorSetAppearanceInstance>(animEvent.SourceEntity))
            {
                var armorSet = SystemAPI.GetComponent<ArmorSetAppearanceInstance>(animEvent.SourceEntity);

                soundPos = SystemAPI.GetComponent<LocalToWorld>(armorSet.Owner).Position;

                if(SystemAPI.HasComponent<AttackVerticalOffset>(armorSet.Owner))
                {
                    var offset = SystemAPI.GetComponent<AttackVerticalOffset>(armorSet.Owner);
                    soundPos += new Unity.Mathematics.float3(0,offset.Value,0);
                }
            }
            else
            {
                soundPos = SystemAPI.GetComponent<LocalToWorld>(animEvent.SourceEntity).Position;
            }

            //UnityEngine.Debug.Log($"swing pos {soundPos.Value}");

            commands.SetComponent(0, groupInstance, LocalTransform.FromPosition(soundPos));
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
