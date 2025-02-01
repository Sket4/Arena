using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct CharacterHead : IComponentData
    {
        [HideInAuthoring]
        public PrefabID ModelID;
    }
    
    public class CharacterHeadComponent : ComponentDataBehaviour<CharacterHead>
    {
    }
}
