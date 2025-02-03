using System;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    [Sync]
    public struct CharacterHairstyle : IComponentData
    {
        [HideInAuthoring]
        public PrefabID ID;
    }
    
    public class CharacterHairstyleComponent : ComponentDataBehaviour<CharacterHairstyle>
    {
    }
}
