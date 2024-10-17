using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using TzarGames.Rendering;
using UnityEngine;

namespace Arena.Client
{
    [UseDefaultInspector]
    public class SingleLightProbeComponent : ComponentDataBehaviour<LightProbeData>
    {
        [ColorUsage(false, true)]
        public Color SkyColor = Color.cyan;
        [ColorUsage(false, true)]
        public Color EnvColor1 = Color.red;
        [ColorUsage(false, true)]
        public Color EnvColor2 = Color.green;
        [ColorUsage(false, true)]
        public Color EnvColor3 = Color.blue;
        [ColorUsage(false, true)]
        public Color EnvColor4 = Color.yellow;
        [ColorUsage(false, true)]
        public Color GroundColor = Color.gray;

        protected override void Bake<K>(ref LightProbeData serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            serializedData.X_color_positive = EnvColor1;
            serializedData.X_color_negative = EnvColor2;
            serializedData.Y_color_positive = SkyColor;
            serializedData.Y_color_negative = GroundColor;
            serializedData.Z_color_positive = EnvColor3;
            serializedData.Z_color_negative = EnvColor4;
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
