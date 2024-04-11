using TzarGames.GameCore;
using Unity.Entities;

namespace Arena.Items
{
    [System.Serializable]
    public struct MainCurrencyDrop : IComponentData
    {
        public uint Min;
        public uint Max;
    }

    [System.Serializable]
    public struct MainCurrency : IComponentData
    {
    }

    public class MainCurrencyDropComponent : ComponentDataBehaviour<MainCurrencyDrop>
    {
        protected override void Bake<K>(ref MainCurrencyDrop serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            baker.AddComponent(new MainCurrency());
        }
    }
}

