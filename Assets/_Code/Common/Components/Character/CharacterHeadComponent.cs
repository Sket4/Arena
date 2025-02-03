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
    
    public class CharacterHeadComponent : ComponentDataBehaviour<CharacterHead>
    {
    }
}
