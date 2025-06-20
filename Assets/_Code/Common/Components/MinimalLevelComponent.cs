using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct MinimalLevel : IComponentData
    {
        public ushort Value;
    }
    
    public class MinimalLevelComponent : ComponentDataBehaviour<MinimalLevel>
    {
    }
}