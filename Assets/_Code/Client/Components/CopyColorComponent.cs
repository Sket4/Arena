using System;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Presentation
{
    [Serializable]
    public struct CopyColor : IBufferElementData
    {
        public Entity Target;
    }
    [UseDefaultInspector]
    public class CopyColorComponent : DynamicBufferBehaviour<CopyColor>
    {
        public Renderer Target;
        public Renderer[] AdditionalTargets;

        protected override void Bake<K>(ref DynamicBuffer<CopyColor> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            if (Target)
            {
                serializedData.Add(new CopyColor { Target = baker.GetEntity(Target)});
            }

            if (AdditionalTargets != null)
            {
                foreach (var target in AdditionalTargets)
                {
                    if (target)
                    {
                        serializedData.Add(new CopyColor { Target = baker.GetEntity(target)});
                    }
                }
            }
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
