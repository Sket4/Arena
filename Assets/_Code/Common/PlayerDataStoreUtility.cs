using TzarGames.MatchFramework;
using TzarGames.GameCore;
using TzarGames.GameCore.Abilities;
using Unity.Entities;

namespace Arena
{

    public static class PlayerDataStoreUtility
    {
        static void addBoolPair(ref MetaData data, ItemMetaKeys key, bool value)
        {
            if (data == null) data = new MetaData();
            data.BoolKeyValues.Add(new BoolKeyValuePair { Key = key.ToString(), Value = value });
        }

        static object createDataFromItemEntity(Entity itemEntity, EntityManager manager)
        {
            var item = manager.GetComponentData<Item>(itemEntity);

            UniqueID instanceID = default;

            if (manager.HasComponent<UniqueID>(itemEntity))
            {
                instanceID = manager.GetComponentData<UniqueID>(itemEntity);
            }

            MetaData itemData = null;

            if(manager.HasComponent<ActivatedState>(itemEntity))
            {
                addBoolPair(ref itemData, ItemMetaKeys.Activated, manager.GetComponentData<ActivatedState>(itemEntity).Activated);
            }

            if (manager.HasComponent<Consumable>(itemEntity))
            {
                return new ConsumableItemData
                {
                    Count = manager.GetComponentData<Consumable>(itemEntity).Count,
                    TypeID = item.ID,
                    ID = instanceID.Value,
                    Data = itemData
                };
            }
            else
            {
                return new ItemData
                {
                    ID = instanceID.Value,
                    TypeID = item.ID,
                    Data = itemData
                };
            }
        }

        public static CharacterData CreateCharacterDataFromEntity(Entity entity, EntityManager manager)
        {
            var data = new CharacterData();

            data.ID = (int)manager.GetComponentData<UniqueID>(entity).Value;
            data.Name = manager.GetComponentData<Name30>(entity).ToString();
            data.XP = (int)manager.GetComponentData<XP>(entity).Value;
            data.Class = (int)manager.GetComponentData<CharacterClassData>(entity).Value;
            
            data.ItemsData = new ItemsData();
            
            // TODO bag index?
            data.ItemsData.Bags.Add(new ItemBagData());
            var bag = data.ItemsData.Bags[0];

            if (manager.HasComponent<InventoryElement>(entity))
            {
                var inventory = manager.GetBuffer<InventoryElement>(entity);

                for (int i = 0; i < inventory.Length; i++)
                {
                    var itemElement = inventory[i];
                    
                    var itemData = createDataFromItemEntity(itemElement.Entity, manager);
                    
                    if(itemData is ConsumableItemData)
                    {
                        bag.ConsumableItems.Add(itemData as ConsumableItemData);
                    }
                    else
                    {
                        bag.Items.Add(itemData as ItemData);
                    }
                }
            }

            if(manager.HasComponent<AbilityElement>(entity))
            {
                var abilities = manager.GetBuffer<AbilityElement>(entity);

                for(int i=0; i<abilities.Length; i++)
                {
                    var ability = abilities[i].AbilityEntity;
                    var serializedData = new AbilityData();
                    serializedData.TypeID = manager.GetComponentData<AbilityID>(ability).Value;
                    //serializedData.ID = manager.GetComponentData<UniqueID>(ability).Value;
                    serializedData.Data = null;

                    if(data.AbilityData == null)
                    {
                        data.AbilityData = new AbilitiesData();
                    }
                    data.AbilityData.Abilities.Add(serializedData);
                }
            }
            
            var progressEntity = manager.GetComponentData<CharacterGameProgressReference>(entity).Value;
            var progress = manager.GetComponentData<CharacterGameProgress>(progressEntity);

            if (data.Progress == null)
            {
                data.Progress = new GameProgress();
            }
            data.Progress.CurrentStage = progress.CurrentStage;
            data.Progress.CurrentBaseLocation = progress.CurrentBaseLocationID;
            data.Progress.CurrentBaseLocationSpawnPoint = progress.CurrentBaseLocationSpawnPointID;
            
            var progressFlags = manager.GetBuffer<CharacterGameProgressFlags>(progressEntity);

            foreach (var progressFlag in progressFlags)
            {
                data.Progress.Flags.Add(progressFlag.Value);    
            }

            var progressKV = manager.GetBuffer<CharacterGameProgressKeyValue>(progressEntity);
            foreach (var keyValue in progressKV)
            {
                data.Progress.KeyValueStorage.Add(new GameProgressKeyValue
                {
                    Key = keyValue.Key,
                    Value = keyValue.Value
                });
            }

            var quests = manager.GetBuffer<CharacterGameProgressQuests>(progressEntity);
            foreach (var quest in quests)
            {
                data.Progress.Quests.Add(new QuestEntry
                {
                    ID = quest.QuestID,
                    State = quest.QuestState
                });
            }
            
            return data;
        }
    }
}
