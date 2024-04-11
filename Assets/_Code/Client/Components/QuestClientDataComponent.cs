using TzarGames.Common;
using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;

namespace Arena.Client
{
    [System.Serializable]
    public sealed class QuestClientData : IComponentData
    {
        public LocalizedStringAsset Name;
        public LocalizedStringAsset Description;
    }
    
    public class QuestClientDataComponent : ComponentDataClassBehaviour<QuestClientData>
    {
        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
