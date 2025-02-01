using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arena;
using Arena.Quests;
using Arena.Server;
using JetBrains.Annotations;
using TzarGames.GameCore;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Entities;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Arena.Client
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(GameCommandBufferSystem))]
    public partial class ArenaPlayerDataLocalStoreSystem : PlayerDataLocalStoreBaseSystem
    {        
        public CharacterClass DebugCharacterClass = CharacterClass.Knight;
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
                
                var gender = random.NextBool() ? Genders.Female : Genders.Male;
                int headID;

                switch (gender)
                {
                    case Genders.Male:
                        headID = Identifiers.DefaultMaleHeadID;
                        break;
                    case Genders.Female:
                        headID = Identifiers.DefaultFemaleHeadID;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                data = SharedUtility.CreateDefaultCharacterData(DebugCharacterClass, "Player", gender, headID);
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
