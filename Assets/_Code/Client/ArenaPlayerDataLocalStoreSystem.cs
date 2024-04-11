using System.Collections.Generic;
using System.Threading.Tasks;
using Arena;
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
                data = SharedUtility.CreateDefaultCharacterData(DebugCharacterClass, "Player");
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
