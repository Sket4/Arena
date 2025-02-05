using TzarGames.MatchFramework.Database.Server;
using System.Collections.Generic;
using Arena;
using TzarGames.MatchFramework;
using Google.Protobuf;

namespace DatabaseApp.DB
{
    public class GameData
    {
        public int Id { get; set; }
        public DbAccount Account { get; set; }

        public Character SelectedCharacter { get; set; }
        public List<Character> Characters { get; set; } = new List<Character>();
    }

    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int XP { get; set; }
        public int Class { get; set; }
        public int HeadID { get; set; }
        public int HairstyleID { get; set; }
        public int SkinColor { get; set; }
        public int HairColor { get; set; }
        public int EyeColor { get; set; }
        public byte[] AbilityData { get; set; }
        public byte[] ItemData { get; set; }

        // game specific data
        public byte[] GameProgress;
    }

    public class DatabaseConversion
    {
        public static Character ConvertToDbCharacter(CharacterData characterData)
        {
            if (characterData == null)
            {
                return null;
            }

            byte[] itemBytes = null;

            if(characterData.ItemsData != null)
            {
                itemBytes = characterData.ItemsData.ToByteArray();
            }


            byte[] abilityBytes = null;

            if (characterData.AbilityData != null)
            {
                abilityBytes = characterData.AbilityData.ToByteArray();
            }

            var progress = characterData.Progress;
            if(progress == null)
            {
                progress = new GameProgress();
            }

            var character = new Character
            {
                Id = characterData.ID,
                Name = characterData.Name,
                Class = characterData.Class,
                XP = characterData.XP,
                ItemData = itemBytes,
                AbilityData = abilityBytes,
                
                HeadID = characterData.HeadID,
                HairColor = characterData.HairColor,
                HairstyleID = characterData.HairstyleID,
                SkinColor = characterData.SkinColor,
                EyeColor = characterData.EyeColor,

                GameProgress = progress.ToByteArray(),
            };

            return character;
        }

        public static Arena.GameData ConvertToGameData(GameData dbGameData)
        {
            var result = new Arena.GameData();

            result.ID = dbGameData.Id;
            result.SelectedCharacterName = null;
            
            if(dbGameData.Characters != null)
            {
                for (int i = 0; i < dbGameData.Characters.Count; i++)
                {
                    var dbCharacter = dbGameData.Characters[i];

                    if (dbCharacter.Id == dbGameData.SelectedCharacter.Id)
                    {
                        result.SelectedCharacterName = dbCharacter.Name;
                    }
                    var character = ConvertToCharacterData(dbCharacter);
                    result.Characters.Add(character);
                }
            }

            return result;
        }

        public static CharacterData ConvertToCharacterData(DB.Character dbCharacter)
        {
            if (dbCharacter == null)
            {
                return null;
            }

            var newCharacter = new CharacterData();
            newCharacter.ID = dbCharacter.Id;
            newCharacter.XP = dbCharacter.XP;
            newCharacter.Class = dbCharacter.Class;
            newCharacter.Name = dbCharacter.Name;
            newCharacter.EyeColor = dbCharacter.EyeColor;
            newCharacter.SkinColor = dbCharacter.SkinColor;
            newCharacter.HeadID = dbCharacter.HeadID;
            newCharacter.HairstyleID = dbCharacter.HairstyleID;
            newCharacter.HairColor = dbCharacter.HairColor;

            if (dbCharacter.ItemData != null)
            {
                newCharacter.ItemsData = ItemsData.Parser.ParseFrom(dbCharacter.ItemData);
            }

            if (dbCharacter.AbilityData != null)
            {
                newCharacter.AbilityData = AbilitiesData.Parser.ParseFrom(dbCharacter.AbilityData);
            }

            if(dbCharacter.GameProgress != null)
            {
                newCharacter.Progress = GameProgress.Parser.ParseFrom(dbCharacter.GameProgress);
            }

            return newCharacter;
        }
    }
}
