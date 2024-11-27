using System;
using TzarGames.GameCore;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    [MaterialProperty("_SkinColor")]
    public struct SkinColor : IComponentData
    {
        public Color Value;
    }
    
    public class SkinColorComponent : ComponentDataBehaviour<SkinColor>
    {
    }
}
