using TzarGames.GameCore;
using TzarGames.GameCore.Client;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct MusicMixerSettings : IComponentData
    {
        public float TransitionTime;
        public Entity BattleMusicClipsGroup;
        public Entity PeaceMusicClipsGroup;
        public float MusicVolumeFactor;

        [System.NonSerialized] public bool IsInTransition;
        [System.NonSerialized] public bool IsClipSwitched;
        [System.NonSerialized] public float TransitionStartTime;
        [System.NonSerialized] public bool IsInBattle;
        [System.NonSerialized] public int BattleMusicCounter;
        [System.NonSerialized] public int PeaceMusicCounter;
        [System.NonSerialized] public float BattleMusicPlayPosition;
        [System.NonSerialized] public float PeaceMusicPlayPosition;

        [System.NonSerialized] public bool IsWaitingForClipLoad;
        [System.NonSerialized] public WeakObjectReference<AudioClip> LoadingClipReference;
    }

    [UseDefaultInspector]
    public class MusicMixerComponent : ComponentDataBehaviour<MusicMixerSettings>
    {
        public float TransitionTime = 1;
        public float MusicVolumeFactor = 1;
        public SoundGroupComponent BattleClipsGroup;
        public SoundGroupComponent PeaceMusicClipsGroup;

        protected override void Bake<K>(ref MusicMixerSettings serializedData, K baker)
        {
            serializedData.TransitionTime = TransitionTime;
            serializedData.BattleMusicClipsGroup = baker.GetEntity(BattleClipsGroup);
            serializedData.PeaceMusicClipsGroup = baker.GetEntity(PeaceMusicClipsGroup);
            serializedData.BattleMusicCounter = -1;
            serializedData.PeaceMusicCounter = -1;
            serializedData.MusicVolumeFactor = MusicVolumeFactor;
        }
    }
}
