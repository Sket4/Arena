using System;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    [Sync]
    public struct CharacterHead : IComponentData
    {
        [HideInAuthoring]
        public PrefabID ModelID;
    }

    [Serializable]
    [Sync]
    public struct CharacterHairColor : IComponentData
    {
        public PackedColor Value;
    }

    [Serializable]
    [Sync]
    public struct CharacterEyeColor : IComponentData
    {
        public PackedColor Value;
    }
    
    [Serializable]
    [Sync]
    public struct CharacterHairstyle : IComponentData
    {
        [HideInAuthoring]
        public PrefabID ID;
    }
    
    [Serializable]
    [Sync]
    public struct CharacterSkinColor : IComponentData
    {
        [HideInAuthoring]
        public PackedColor Value;
    }

    public class CharacterVisualComponent : ComponentDataBehaviour<CharacterHead>
    {
        protected override void Bake<K>(ref CharacterHead serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent(new CharacterEyeColor { Value = PackedColor.White });
            baker.AddComponent(new CharacterHairColor { Value = PackedColor.White });
            baker.AddComponent(new CharacterHairstyle());
            baker.AddComponent(new CharacterSkinColor { Value = PackedColor.White });
        }
    }
}
