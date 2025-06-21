using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct MaximumLevel : IComponentData
    {
        public ushort Value;
    }
    
    public class MaximumLevelComponent : ComponentDataBehaviour<MaximumLevel>
    {
    }
}