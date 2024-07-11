using TzarGames.MatchFramework;

namespace Arena
{
    public class CharacterTemplate
    {
        public int BaseArmorID;
        public int BaseWeaponID;

        public int[] AbilityIDs;
    }

    public static class Identifiers
    {
        public static readonly CharacterTemplate Archer = new CharacterTemplate
        {
            BaseArmorID = 35,
            BaseWeaponID = 42,
            AbilityIDs = new int[]
            {
                31, // главная атака
                20, // усиленная атака
                21, // рывок
                22, // круговая атака
                23, // боевое лечение
            }
        };

        public static readonly CharacterTemplate Knight = new CharacterTemplate
        {
            BaseArmorID = 36,
            BaseWeaponID = 38,
            AbilityIDs = new int[]
            {
                19, // главная атака
                20, // усиленная атака
                21, // рывок
                22, // круговая атака
                23, // боевое лечение
            }
        };

        public static readonly int DefaultStartLocationID = 56;     // port village
        public static readonly int DefaultStartLocationSpawnPointID = 134; // причал

        public static readonly int[] SafeZoneLocations = new int[]
        {
            63 // main city
        };
    }

    public static class MetaDataKeys
    {
        public const string SceneId = "ARENA_SCENE_ID";
        public const string SpawnPointId = "ARENA_SPAWNPOINT_ID";
        public const string MultiplayerGame = "ARENA_MULTIPLAYER_GAME";
    }
        

    public static class SharedUtility
    {
        public const string ErrorKey = "error";
        public const string TokenExpiredValue = "token_expired";

        public static bool IsSafeZoneLocation(int locationID)
        {
            foreach(var id in Identifiers.SafeZoneLocations)
            {
                if(id == locationID)
                {
                    return true;
                }
            }
            return false;
        }

        public static CharacterData CreateDefaultCharacterData(CharacterClass characterClass, string characterName)
        {
            var data = new CharacterData();
            data.Name = characterName;
            data.Class = (int)characterClass;

            data.Progress = new GameProgress
            {
                CurrentBaseLocation = Identifiers.DefaultStartLocationID,
                CurrentBaseLocationSpawnPoint = Identifiers.DefaultStartLocationSpawnPointID,
                CurrentStage = 0,
            };

            switch (characterClass)
            {
                case CharacterClass.Knight:
                    initCharacterData(data, Identifiers.Knight);
                    break;
                case CharacterClass.Mage:
                    break;
                case CharacterClass.Archer:
                    initCharacterData(data, Identifiers.Archer);
                    break;
            }

            return data;
        }

        public static GameData CreateDefaultGameData()
        {
            var data = new GameData();
            return data;
        }

        static void initCharacterData(CharacterData data, CharacterTemplate template)
        {
            if(data.ItemsData == null)
            {
                data.ItemsData = new ItemsData();
                data.ItemsData.Bags.Add(new ItemBagData());
            }

            addItem(data, template.BaseArmorID, 0, true);
            addItem(data, template.BaseWeaponID, 0, true);

            if(data.AbilityData == null)
            {
                data.AbilityData = new AbilitiesData();
            }
            foreach (var abilityId in template.AbilityIDs)
            {
                addAbility(data, abilityId);
            }
        }

        static void addAbility(CharacterData data, int typeId)
        {
            var abilityData = new AbilityData();
            abilityData.TypeID = typeId;
            data.AbilityData.Abilities.Add(abilityData);
        }

        static void addItem(CharacterData data, int typeId, int bagIndex, bool activated = false)
        {
            if(data.ItemsData.Bags.Count <= bagIndex)
            {
                throw new System.ArgumentOutOfRangeException($"no bag with index {bagIndex}");
            }

            MetaData itemData = null;

            if (activated)
            {
                if (itemData == null) itemData = new MetaData();
                itemData.BoolKeyValues.Add(ItemMetaKeys.Activated.ToString(), true);
            }

            data.ItemsData.Bags[bagIndex].Items.Add(new ItemData { TypeID = typeId, Data = itemData });
        }
    }
}
