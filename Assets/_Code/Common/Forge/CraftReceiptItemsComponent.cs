using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [System.Serializable]
    public struct CraftReceiptItems : IBufferElementData
    {
        public Entity Item;
        public uint Count;
    }
    
    [UseDefaultInspector]
    public class CraftReceiptItemsComponent : DynamicBufferBehaviour<CraftReceiptItems>
    {
        [System.Serializable]
        class CraftReceiptItem
        {
            public ItemKey Item;
            public uint Count;
        }
            
        [SerializeField]
        private CraftReceiptItem[] requiredItems;

        protected override void Bake<K>(ref DynamicBuffer<CraftReceiptItems> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            foreach (var item in requiredItems)
            {
                var data = new CraftReceiptItems
                {
                    Item = baker.ConvertObjectKey(item.Item),
                    Count = item.Count
                };
                serializedData.Add(data);
            }
        }
    }
}
