using TzarGames.GameCore;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization;

namespace Arena
{
    [System.Serializable]
    public struct StoreItems : IBufferElementData
    {
        public int ItemID;
        public byte GroupID;
    }

    [System.Serializable]
    public struct StoreGroups : IBufferElementData
    {
        public long LocalizationID;
    }

    [UseDefaultInspector]
    public class StoreItemsComponent : DynamicBufferBehaviour<StoreItems>
    {
        [System.Serializable]
        class StoreItemGroupAuthoring
        {
            public string Name;
            public LocalizedString GroupName;
            public StoreItemAuthoring[] Items;
        }

        [System.Serializable]
        class StoreItemAuthoring
        {
            public ItemKey ItemID;
        }
        
        [SerializeField] private StoreItemGroupAuthoring[] groups;

        protected override void Bake<K>(ref DynamicBuffer<StoreItems> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            DynamicBuffer<StoreGroups> groupsBuffer;

            if (ShouldBeConverted(ConversionTargetOptions.LocalAndClient, baker))
            {
                groupsBuffer = baker.AddBuffer<StoreGroups>();
            }
            else
            {
                groupsBuffer = default;
            }

            for (byte index = 0; index < groups.Length; index++)
            {
                var group = groups[index];

                if (groupsBuffer.IsCreated)
                {
                    groupsBuffer.Add(new StoreGroups
                    {
                        LocalizationID = group.GroupName != null ? group.GroupName.TableEntryReference.KeyId : 0 
                    });
                }
                
                foreach (var item in group.Items)
                {
                    if (item.ItemID == null)
                    {
                        Debug.LogError($"null store item at {name}");
                        continue;
                    }
                    serializedData.Add(new StoreItems { ItemID = item.ItemID.Id, GroupID = index });
                }
            }
        }
    }
}
