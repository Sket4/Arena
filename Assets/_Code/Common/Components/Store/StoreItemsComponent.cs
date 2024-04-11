using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [System.Serializable]
    public struct StoreItems : IBufferElementData
    {
        public int ItemID;
    }

    [UseDefaultInspector]
    public class StoreItemsComponent : DynamicBufferBehaviour<StoreItems>
    {
        [SerializeField] private ItemKey[] items;

        protected override void Bake<K>(ref DynamicBuffer<StoreItems> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            
            foreach (var item in items)
            {
                serializedData.Add(new StoreItems {ItemID = item.Id});
            }
        }
    }
}
