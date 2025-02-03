using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using TzarGames.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    [MaterialProperty("_SkinColor")]
    public struct SkinColor : IComponentData
    {
        public Color Value;

        public SkinColor(PackedColor packedColor)
        {
            Value = new Color32(packedColor.r, packedColor.g, packedColor.b, packedColor.a);
        }
    }
    
    public class SkinColorComponent : ComponentDataBehaviour<SkinColor>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }

        protected override SkinColor CreateDefaultValue()
        {
            return new SkinColor
            {
                Value = Color.white
            };
        }
    }
}
