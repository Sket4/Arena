using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [System.Serializable]
    public struct ItemPrefabs : IComponentData
    {
        public int MainCurrencyID;
        public Entity MainCurrencyPrefab;
    }

    [UseDefaultInspector]
    public class ItemPrefabIDsComponent : ComponentDataBehaviour<ItemPrefabs>
    {
        public ItemKey MainCurrencyKey;

        protected override void Bake<K>(ref ItemPrefabs serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if(MainCurrencyKey != null)
            {
                serializedData.MainCurrencyID = MainCurrencyKey.Id;
                serializedData.MainCurrencyPrefab = baker.ConvertObjectKey(MainCurrencyKey);
            };
        }
    }
}