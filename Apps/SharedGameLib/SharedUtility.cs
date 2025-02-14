using System.Runtime.InteropServices;
using System;
using TzarGames.MatchFramework;

namespace Arena
{
    public class CharacterTemplate
    {
        public int BaseArmorID_Female;
        public int BaseArmorID_Male;
        public int BaseWeaponID;

        public int[] AbilityIDs;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PackedColor
    {
        [FieldOffset(0)]
        public int rgba;

        [FieldOffset(0)]
        public byte r;

        [FieldOffset(1)]
        public byte g;

        [FieldOffset(2)]
        public byte b;

        [FieldOffset(3)]
        public byte a;

        public PackedColor(byte r, byte g, byte b, byte a)
        {
            this.rgba = 0;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public PackedColor(byte r, byte g, byte b)
        {
            this.rgba = 0;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 255;
        }
        public PackedColor(int rgba)
        {
            r = g = b = a = 0;
            this.rgba = rgba;
        }

        public static readonly PackedColor White = new PackedColor(255,255,255);
        public static readonly PackedColor Black = new PackedColor(0, 0, 0);
        public static readonly PackedColor Clear = new PackedColor(0, 0, 0, 0);
    }

    public static class Identifiers
    {
        public static readonly CharacterTemplate Archer = new CharacterTemplate
        {
            BaseArmorID_Male = 35,
            BaseArmorID_Female = 35, // TODO
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
            BaseArmorID_Male = 36,
            BaseArmorID_Female = 118,
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

        public static readonly int DefaultMaleHeadID = 115;
        public static readonly int DefaultFemaleHeadID = 116;

        public static readonly int[] MaleHeadIDs = new int[]
        {
            115,
            124,
            125,
            126,
            127,
            128
        };
        public static readonly int[] FemaleHeadIDs = new int[]
        {
            116,
            119,
            120,
            121,
            122,
            123
        };

        public static readonly int[] SafeZoneLocations = new int[]
        {
            63 // main city
        };

        public static readonly int[] MaleHairStyles = new int[]
        {
            130,
            131
        };

        public static readonly int[] FemaleHairStyles = new int[]
        {
            129
        };

        public static readonly PackedColor[] SkinColors = new PackedColor[]
        {
            new PackedColor(255,255,255),
            new PackedColor(20,14,14),
            new PackedColor(68,47,43),
            new PackedColor(255,190,178),
            new PackedColor(128,128,128),
            new PackedColor(145,128,60),
        };

        public static readonly PackedColor[] EyeColors = new PackedColor[]
        {
            new PackedColor(100,100,100),       // серый
            new PackedColor(80,105,40),       // зеленый
            new PackedColor(32,42,16),          // темно зеленый
            new PackedColor(26,59,83),       // синий
            new PackedColor(59,101,129),       // голубой
            new PackedColor(96,47,21),       // карий
            new PackedColor(41,20,10),       // т.карий
            new PackedColor(21,11,5),       // черный
        };

        public static readonly PackedColor[] HairColors = new PackedColor[]
        {
            new PackedColor(255,255,255),       // белый
            new PackedColor(8,8,8),             // черный
            new PackedColor(57,57,57),             // серый
            new PackedColor(117,46,0),             // рыжий
            new PackedColor(145,80,58),             // св. рыжий
            new PackedColor(65,19,4),             // темно рыжий
            new PackedColor(28,20,10),             // русый
            new PackedColor(122,94,56),             // св. русый
            new PackedColor(22,10,0),             // шатен
            new PackedColor(241,211,116),             // блонд
            new PackedColor(254,231,162),             // блонд 2
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

        public static CharacterData CreateDefaultCharacterData(CharacterClass characterClass, string characterName, Genders gender, int headID, int hairstyleID, int skinColor, int hairColor, int eyeColor, int armorColor)
        {
            var data = new CharacterData();
            data.Name = characterName;
            data.Class = (int)characterClass;
            data.HeadID = headID;
            data.Gender = gender;
            data.HairstyleID = hairstyleID;
            data.SkinColor = skinColor;
            data.HairColor = hairColor;
            data.EyeColor = eyeColor;

            data.Progress = new GameProgress
            {
                CurrentBaseLocation = Identifiers.DefaultStartLocationID,
                CurrentBaseLocationSpawnPoint = Identifiers.DefaultStartLocationSpawnPointID,
                CurrentStage = 0,
            };

            switch (characterClass)
            {
                case CharacterClass.Knight:
                    initCharacterData(data, Identifiers.Knight, armorColor);
                    break;
                case CharacterClass.Mage:
                    break;
                case CharacterClass.Archer:
                    initCharacterData(data, Identifiers.Archer, armorColor);
                    break;
            }

            return data;
        }

        public static GameData CreateDefaultGameData()
        {
            var data = new GameData();
            return data;
        }

        static void initCharacterData(CharacterData data, CharacterTemplate template, int armorColor)
        {
            if(data.ItemsData == null)
            {
                data.ItemsData = new ItemsData();
                data.ItemsData.Bags.Add(new ItemBagData());
            }

            var isMale = data.Gender == Genders.Male;

            var armorItem = addItem(data, isMale ? template.BaseArmorID_Male : template.BaseArmorID_Female, 0, true);
            armorItem.Data.IntKeyValues.Add(new IntKeyValuePair { Key = ItemMetaKeys.Color.ToString(), Value = armorColor });

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

        static ItemData addItem(CharacterData data, int typeId, int bagIndex, bool activated = false)
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

            var result = new ItemData { TypeID = typeId, Data = itemData };
            data.ItemsData.Bags[bagIndex].Items.Add(result);

            return result;
        }
    }
}
