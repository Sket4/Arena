using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities.Content;
using System.Threading.Tasks;

namespace Arena.Client
{
    sealed class AudioSourceCleanup : ICleanupComponentData
    {
        public AudioSource Source;
    }

    [DisableAutoCreation]
    public partial class MusicMixerSystem : GameSystemBase
    {
        bool isDestroyed = false;
        private EntityQuery mixerSettingsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            mixerSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<MusicMixerSettings>());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            isDestroyed = true;
        }

        async void playClipRefAsync(WeakObjectReference<AudioClip> clipRef, AudioSource audioSource, float playbackPosition)
        {
            if(clipRef.LoadingStatus == ObjectLoadingStatus.None)
            {
                clipRef.LoadAsync();
            }

            float startLoadTime = UnityEngine.Time.unscaledTime;

            while(clipRef.LoadingStatus != ObjectLoadingStatus.Completed && isDestroyed == false)
            {
                if (UnityEngine.Time.unscaledTime - startLoadTime >= 3)
                {
                    Debug.LogError($"Reached max wait time for loading clip reference {clipRef}");
                    return;
                }
                
                await Task.Yield();

                if(clipRef.LoadingStatus == ObjectLoadingStatus.Error)
                {
                    Debug.LogError($"Failed to load audio clip with id {clipRef}");
                    return;
                }
            }

            if (isDestroyed)
            {
                return;
            }

            audioSource.clip = clipRef.Result;
            audioSource.time = math.clamp(playbackPosition, 0, audioSource.clip.length);
            audioSource.Play();
        }

        protected override void OnSystemUpdate()
        {
            var time = World.Time.ElapsedTime;

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<AudioSourceCleanup>()
                .WithAll<MusicMixerSettings>()
                .ForEach((Entity entity, ref MusicMixerSettings mixer) =>
                {
                    var musicMixerAudioSourceObj = new GameObject("Music mixer");
                    var audioSource = musicMixerAudioSourceObj.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 0;
                    audioSource.loop = false;

                    EntityManager.AddComponentData(entity, new AudioSourceCleanup
                    {
                        Source = audioSource
                    });
                    EntityManager.AddComponentObject(entity, audioSource);

                    mixer.TransitionStartTime = ((float)time) - mixer.TransitionTime * 0.5f;
                    mixer.IsInTransition = true;

                }).Run();

            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<AudioSourceCleanup>()
                .WithNone<MusicMixerSettings>()
                .ForEach((Entity entity, AudioSourceCleanup reference) =>
                {
                    Object.Destroy(reference.Source.gameObject);
                    EntityManager.RemoveComponent<AudioSourceCleanup>(entity);

                }).Run();

            
            
            if (SystemAPI.TryGetSingletonEntity<LocalPlayerTag>(out Entity localPlayerEntity))
            {
                Entities
                    .WithoutBurst()
                    .WithChangeFilter<EnemyDetectionData>()
                    .ForEach((in EnemyDetectionData enemyDetectionData, in PlayerController controller) =>
                    {
                        if (controller.Value != localPlayerEntity)
                        {
                            return;
                        }

                        if(mixerSettingsQuery.TryGetSingleton(out MusicMixerSettings globalMixerSettings) == false)
                        {
                            return;
                        }

                        if (globalMixerSettings.IsInBattle != enemyDetectionData.HasNearEnemy)
                        {
                            globalMixerSettings.IsInBattle = enemyDetectionData.HasNearEnemy;
                            globalMixerSettings.IsInTransition = true;
                            globalMixerSettings.TransitionStartTime = (float)time;
                            
                            mixerSettingsQuery.SetSingleton(globalMixerSettings);
                        }

                    }).Run();
            }

            Entities
                .WithoutBurst()
                .ForEach((AudioSource audioSource, ref MusicMixerSettings musicMixerSettings) =>
                {
                    if(musicMixerSettings.IsWaitingForClipLoad)
                    {
                        if(musicMixerSettings.LoadingClipReference.LoadingStatus == ObjectLoadingStatus.Completed)
                        {
                            musicMixerSettings.IsWaitingForClipLoad = false;
                        }
                        else if(musicMixerSettings.LoadingClipReference.LoadingStatus == ObjectLoadingStatus.Error)
                        {
                            musicMixerSettings.IsWaitingForClipLoad = false;
                        }
                    }

                    if (musicMixerSettings.BattleMusicCounter == -1)
                    {
                        var battleClips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.BattleMusicClipsGroup);
                        musicMixerSettings.BattleMusicCounter = UnityEngine.Random.Range(0, battleClips.Length);
                    }
                    if (musicMixerSettings.PeaceMusicCounter == -1)
                    {
                        var peaceClips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.PeaceMusicClipsGroup);
                        musicMixerSettings.PeaceMusicCounter = UnityEngine.Random.Range(0, peaceClips.Length);
                    }

                    if (audioSource.isPlaying == false && musicMixerSettings.IsInTransition == false)
                    {
                        DynamicBuffer<AudioClipReference> clips;
                        int counter;

                        if(musicMixerSettings.IsInBattle)
                        {
                            clips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.BattleMusicClipsGroup);
                            counter = musicMixerSettings.BattleMusicCounter;
                        }
                        else
                        {
                            clips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.PeaceMusicClipsGroup);
                            counter = musicMixerSettings.PeaceMusicCounter;
                        }

                        counter++;

                        if (counter >= clips.Length)
                        {
                            counter = 0;
                        }

                        var clipRef = clips[counter].Value;

                        if(clipRef.LoadingStatus != ObjectLoadingStatus.Completed)
                        {
                            musicMixerSettings.IsWaitingForClipLoad = true;
                            musicMixerSettings.LoadingClipReference = clipRef;
                        }
                        playClipRefAsync(clipRef, audioSource, 0);

                        if (musicMixerSettings.IsInBattle)
                        {
                            musicMixerSettings.BattleMusicCounter = counter;
                            musicMixerSettings.BattleMusicPlayPosition = 0;
                        }
                        else
                        {
                            musicMixerSettings.PeaceMusicCounter = counter;
                            musicMixerSettings.PeaceMusicPlayPosition = 0;
                        }
                    }

                    if(musicMixerSettings.IsInTransition)
                    {
                        var elapsedTransitionTime = time - musicMixerSettings.TransitionStartTime;
                        var halfTime = musicMixerSettings.TransitionTime * 0.5f;

                        if(elapsedTransitionTime >= halfTime)
                        {
                            if(musicMixerSettings.IsClipSwitched == false)
                            {
                                musicMixerSettings.IsClipSwitched = true;

                                DynamicBuffer<AudioClipReference> clips;
                                int counter;
                                float playPos;

                                if(musicMixerSettings.IsInBattle)
                                {
                                    clips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.BattleMusicClipsGroup);
                                    counter = musicMixerSettings.BattleMusicCounter;
                                    playPos = musicMixerSettings.BattleMusicPlayPosition;
                                    musicMixerSettings.PeaceMusicPlayPosition = audioSource.time;
                                    //musicMixerSettings.BattleMusicPlayPosition = 0;
                                }
                                else
                                {
                                    clips = SystemAPI.GetBuffer<AudioClipReference>(musicMixerSettings.PeaceMusicClipsGroup);
                                    counter = musicMixerSettings.PeaceMusicCounter;
                                    playPos = musicMixerSettings.PeaceMusicPlayPosition;
                                    musicMixerSettings.BattleMusicPlayPosition = audioSource.time;
                                    //musicMixerSettings.PeaceMusicPlayPosition = 0;
                                }

                                var clipRef = clips[counter].Value;

                                if (clipRef.LoadingStatus != ObjectLoadingStatus.Completed)
                                {
                                    musicMixerSettings.IsWaitingForClipLoad = true;
                                    musicMixerSettings.LoadingClipReference = clipRef;
                                }

                                playClipRefAsync(clipRef, audioSource, playPos);
                                //else
                                //{
                                //    Debug.LogError("Does not have asset managed reference");
                                //}
                            }

                            var volume = math.saturate((elapsedTransitionTime - halfTime) / halfTime);
                            audioSource.volume = (float)volume * musicMixerSettings.MusicVolumeFactor;
                        }
                        else
                        {
                            var volume = 1.0f - math.saturate(elapsedTransitionTime / halfTime);
                            audioSource.volume = (float)volume * musicMixerSettings.MusicVolumeFactor;
                        }

                        if(elapsedTransitionTime >= musicMixerSettings.TransitionTime)
                        {
                            musicMixerSettings.IsInTransition = false;
                            musicMixerSettings.IsClipSwitched = false;
                        }
                    }

                }).Run();
        }
    }
}
