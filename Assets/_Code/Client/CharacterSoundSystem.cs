using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(GameCommandBufferSystem))]
    [UpdateBefore(typeof(AnimationCommandBufferSystem))]
    [UpdateAfter(typeof(ApplyDamageSystem))]
    public partial class CharacterSoundSystem : GameSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var commands = CreateEntityCommandBufferParallel();
            var playSoundEventArchetype = SystemAPI.GetSingleton<PlaySoundSystem.Singleton>().PlayerSoundEventArchetype;

            Entities
                .WithChangeFilter<DeathData>()
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<CharacterDeadAudioClip> deadClips, in LivingState livingState, in LocalTransform transform) =>
            {
                if(livingState.WasDeadInCurrentFrame() == false)
                {
                    return;
                }

                if(deadClips.Length == 0)
                {
                    return;
                }

                var playEvent = commands.CreateEntity(entityInQueryIndex, playSoundEventArchetype);
                var clips = commands.SetBuffer<AudioClipReference>(entityInQueryIndex, playEvent);
                
                foreach(var deadClip in deadClips)
                {
                    clips.Add(new AudioClipReference { Value = deadClip.AudioClip });
                }

                var settings = SoundGroupSettings.DefaultRandom;
                settings.SpatializeBlend = 1;
                //settings.SetMinDistance = true;
                //settings.MinDistance = 10;

                float verticalOffset = 0;

                if(SystemAPI.HasComponent<AttackVerticalOffset>(entity))
                {
                    verticalOffset = SystemAPI.GetComponent<AttackVerticalOffset>(entity).Value;
                }

                commands.SetComponent(entityInQueryIndex, playEvent, settings);
                commands.AddComponent(entityInQueryIndex, playEvent, LocalTransform.FromPosition(transform.Position + math.up() * verticalOffset));

            }).Run();
        }
    }
}
