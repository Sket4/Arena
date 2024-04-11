using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using Unity.Mathematics;

namespace Arena.Client
{
    [System.Serializable]
    public struct PlayerSmoothCorrection : IComponentData
    {
        public float CorrectionTime;
        [System.NonSerialized] public bool ShouldCorrect;
        [System.NonSerialized] public double CorrectionStartTime;
    }
    
    public class PlayerSmoothCorrectionComponent : ComponentDataBehaviour<PlayerSmoothCorrection>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return ConversionTargetOptions.Client;
        }

        public override bool AllowConversionTargetCustomization()
        {
            return false;
        }
    }
}
