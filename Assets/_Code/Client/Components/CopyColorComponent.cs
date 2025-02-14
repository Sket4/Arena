using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Presentation
{
    [Serializable]
    public struct CopyColor : IComponentData
    {
        public Entity Target;
    }
    [UseDefaultInspector]
    public class CopyColorComponent : ComponentDataBehaviour<CopyColor>
    {
        public Renderer Target;

        protected override void Bake<K>(ref CopyColor serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.Target = baker.GetEntity(Target);
        }

        public override bool ShouldBeConverted(IGCBaker baker)
        {
            return ShouldBeConverted(ConversionTargetOptions.LocalAndClient, baker.GetSceneConversionTargetOptions());
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return ConversionTargetOptions.LocalAndClient;
        }
    }
}
