using DatabaseApp.DB;
using Arena.Server;
using Arena;
using Grpc.Core;
using System.Threading.Tasks;
using TzarGames.MatchFramework.Database;
using System.Collections.Generic;
using System;
using NLog;
using TzarGames.MatchFramework;
using Google.Protobuf;

namespace DatabaseApp
{
    class GameDatabaseServiceImpl : GameDatabaseService.GameDatabaseServiceBase
    {
        GameDatabase db;
        static Logger log = LogManager.GetCurrentClassLogger();

        public GameDatabaseServiceImpl(GameDatabase db)
        {
            this.db = db;
        }

        public override async Task<GameDataResult> GetGameDataForAccount(GetGameDataForAccountRequest request, ServerCallContext context)
        {
            try
            {
                log.Info($"Trying to get game data for player {request.AccountId.Value}");
                var result = await db.GetGameDataByIdAsync(request.AccountId, true, true);
                var data = DatabaseConversion.ConvertToGameData(result);
                return new GameDataResult { Data = data };
            }
            catch(Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        public override async Task<CreateCharacterResult> CreateCharacterForAccount(CharacterCreateRequest request, ServerCallContext context)
        {
            try
            {
                var hasCharacterWithName = await db.HasCharacterWithName(request.Name);

                if (hasCharacterWithName)
                {
                    return new CreateCharacterResult { Result = DatabaseResultTypes.AlreadyCreated };
                }

                var characterData = Arena.SharedUtility.CreateDefaultCharacterData((CharacterClass)request.Class, request.Name);
                var character = DatabaseConversion.ConvertToDbCharacter(characterData);

                var result = await db.CreateCharacterAsync(request.AccountId, character);

                if (result == null)
                {
                    log.Error($"Failed to create character in DB for account {request.AccountId.Value}");
                    return new CreateCharacterResult { Result = DatabaseResultTypes.UnknownError };
                }

                var chararacterData = DB.DatabaseConversion.ConvertToCharacterData(result.Character);
                return new CreateCharacterResult { Character = characterData, Result = result.Result };
            }
            catch(Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        public override async Task<CharacterDeleteResult> DeleteCharacterForAccount(CharacterDeleteRequest request, ServerCallContext context)
        {
            try
            {
                var success = await db.DeleteCharacterAsync(request.AccountId, request.CharacterName);
                return new CharacterDeleteResult
                {
                    Success = success
                };
            }

            catch(Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }

        public override async Task<GetSelectedResult> GetSelectedCharacterForAccount(GetCharacterRequest request, ServerCallContext context)
        {
            try
            {
                log.Info($"Trying to get selected character for player {request.AccountId.Value}");
                var result = await db.GetSelectedCharacterAsync(request.AccountId);
                var character = DatabaseConversion.ConvertToCharacterData(result);
                return new GetSelectedResult { Character = character };
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return new GetSelectedResult { Character = null };
            }
        }

        public override async Task<SelectResult> SelectCharacterForAccount(DbSelectCharacterRequest request, ServerCallContext context)
        {
            log.Info("Trying to select character");
            await db.SelectCharacterAsync(request.AccountId, request.CharacterName);
            return new SelectResult { Success = true };
        }

        public override async Task<GetCharactersResult> GetCharactersForAccount(GetCharacterRequest request, ServerCallContext context)
        {
            try
            {
                var taskResult = await db.GetAllCharactersAsync(request.AccountId, context.CancellationToken);

                var result = new GetCharactersResult();

                if (taskResult != null)
                {
                    var characters = taskResult;
                    var list = new List<CharacterData>();

                    foreach (var c in characters)
                    {
                        var character = DatabaseConversion.ConvertToCharacterData(c);
                        list.Add(character);
                    }

                    result.Characters.AddRange(list);
                }

                return result;
            }
            catch(Exception ex)
            {
                log.Error(ex);
                return new GetCharactersResult();
            }
        }

        public override async Task<DatabaseResult> SaveCharacters(DbSaveCharactersRequest request, ServerCallContext context)
        {
            try
            {
                var charArray = new DB.Character[request.Characters.Count];

                for (int i = 0; i < request.Characters.Count; i++)
                {
                    CharacterData character = request.Characters[i];
                    charArray[i] = DatabaseConversion.ConvertToDbCharacter(character);
                }

                await db.SaveCharactersAsync(charArray, context.CancellationToken);

                return new DatabaseResult { Success = true };
            }
            catch(Exception ex)
            {
                log.Error(ex);

                if(ex.InnerException != null)
                {
                    log.Error(ex.InnerException);
                }
            }
            return new DatabaseResult { Success = false };
        }
    }
}
