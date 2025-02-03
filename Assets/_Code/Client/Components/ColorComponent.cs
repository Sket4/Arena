using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    [MaterialProperty("_BaseColor")]
    public struct ColorData : IComponentData
    {
        public Color Color;

        public ColorData(PackedColor packedColor)
        {
            Color = new Color32(packedColor.r, packedColor.g, packedColor.b, packedColor.a);
        }
    }
    
    public class ColorComponent : ComponentDataBehaviour<ColorData>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
