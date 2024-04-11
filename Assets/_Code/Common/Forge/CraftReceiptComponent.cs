using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [System.Serializable]
    public struct CraftReceipt : IComponentData
    {
        public Entity Item;
    }

    [UseDefaultInspector]
    public class CraftReceiptComponent : ComponentDataBehaviour<CraftReceipt>
    {
        [SerializeField] private ItemKey item;

        protected override void Bake<K>(ref CraftReceipt serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            serializedData.Item = baker.ConvertObjectKey(item);
        }
    }
}
