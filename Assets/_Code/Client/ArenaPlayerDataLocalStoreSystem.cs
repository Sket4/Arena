using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arena.Server;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Entities;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    public partial class ArenaPlayerDataLocalStoreSystem : PlayerDataLocalStoreBaseSystem
    {        
        public CharacterClass DebugCharacterClass = CharacterClass.Knight;
        public Genders DebugGender = Genders.Male;
        public IEnumerable<CharacterGameProgressQuests> DebugQuests = default;
        public IEnumerable<CharacterGameProgressKeyValue> DebugGameProgressIntKeys = default;

        protected override void GetSaveDataFromRequestEntity(Entity requestEntity, Dictionary<string, object> data)
        {
            var saveData = GetComponent<PlayerSaveData>(requestEntity);
            var characterData = PlayerDataStoreUtility.CreateCharacterDataFromEntity(saveData.CharacterEntity, EntityManager);
            data.Add("CharacterData", characterData);

            // заодно обновляем данные игрока
            var targetEntity = GetComponent<Target>(requestEntity).Value;
            
            if (EntityManager.Exists(targetEntity))
            {
                var playerData = EntityManager.GetComponentData<PlayerData>(targetEntity);
                playerData.Data = characterData;    
            }
        }

        protected override Task<object> LoadPlayerData(AuthorizedUser authorizedUser)
        {
            CharacterData data;
            if(GameState.Instance != null)
            {
                data = GameState.Instance.SelectedCharacter;
            }
            else
            {
                var random = Unity.Mathematics.Random.CreateFromIndex((uint)DateTime.Now.Millisecond);

                int headID;
                int[] hairstyles;

                switch (DebugGender)
                {
                    case Genders.Male:
                        headID = Identifiers.DefaultMaleHeadID;
                        hairstyles = Identifiers.MaleHairStyles;
                        break;
                    case Genders.Female:
                        headID = Identifiers.DefaultFemaleHeadID;
                        hairstyles = Identifiers.FemaleHairStyles;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var hairstyle = hairstyles[random.NextInt(0, hairstyles.Length)];
                var hairColor = new PackedColor((byte)random.NextInt(0,256),(byte)random.NextInt(0,256),(byte)random.NextInt(0,256));

                var skinColor = Identifiers.SkinColors[random.NextInt(0, Identifiers.SkinColors.Length)];
                
                data = SharedUtility.CreateDefaultCharacterData(DebugCharacterClass, "Player", DebugGender, headID, hairstyle, skinColor.rgba, hairColor.rgba);
                if (DebugQuests != null)
                {
                    foreach (var debugQuest in DebugQuests)
                    {
                        data.Progress.Quests.Add(new QuestEntry
                        {
                            ID = debugQuest.QuestID,
                            State = debugQuest.QuestState,
                        });     
                    }
                }

                if (DebugGameProgressIntKeys != null)
                {
                    foreach (var kv in DebugGameProgressIntKeys)
                    {
                        data.Progress.KeyValueStorage.Add(new GameProgressKeyValue
                        {
                            Key = kv.Key,
                            Value = kv.Value
                        });
                    }
                }
            }
            
            return Task.FromResult(data as object);
        }

        protected override Task<object> SavePlayerData(PlayerId playerId, Dictionary<string, object> playerData)
        {
            if(GameState.Instance != null)
            {
                var characterData = playerData["CharacterData"] as CharacterData;

                GameState.Instance.PlayerData.Characters[GameState.Instance.PlayerData.SelectedCharacterIndex] =
                    characterData;
                
                GameState.Instance.SaveLocalGame();
            }
            return Task.FromResult(new object());
        }
    }
}
