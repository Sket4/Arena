using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [System.Serializable]
    public struct CraftReceipts : IBufferElementData
    {
        public Entity Receipt;
    }

    [UseDefaultInspector]
    public class CraftReceiptsComponent : DynamicBufferBehaviour<CraftReceipts>
    {
        [SerializeField]
        private CraftReceiptKey[] receipts;

        protected override void Bake<K>(ref DynamicBuffer<CraftReceipts> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            foreach (var receipt in receipts)
            {
                var data = new CraftReceipts
                {
                    Receipt = baker.ConvertObjectKey(receipt)
                };
                serializedData.Add(data);
            }
        }
    }
}
