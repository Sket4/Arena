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
    }

    public static class MetaDataKeys
    {
        public const string SceneId = "ARENA_SCENE_ID";
        public const string MultiplayerGame = "ARENA_MULTIPLAYER_GAME";
    }
        

    public static class SharedUtility
    {
        public const string ErrorKey = "error";
        public const string TokenExpiredValue = "token_expired";

        public static CharacterData CreateDefaultCharacterData(CharacterClass characterClass, string characterName)
        {
            var data = new CharacterData();
            data.Name = characterName;
            data.Class = (int)characterClass;

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
            data.Progress = new GameProgress
            {
                Stage = 0
            };
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
