using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TzarGames.MatchFramework.Database.Server;
using TzarGames.MatchFramework.Database;
using NLog;
using Arena.Server;
using Arena;

namespace DatabaseApp.DB
{
    public class Db_CreateCharacterResult
    {
        public DatabaseResultTypes Result;
        public Character Character;
    }

    public class GameDatabase : Database
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public GameDatabase(bool useDebugDatabase, DatabaseConnectionSettings databaseConnectionSettings) : base(useDebugDatabase, databaseConnectionSettings)
        {
        }

        protected override BaseDatabaseContext CreateContext()
        {
            return new GameDatabaseContext();
        }

        protected override BaseDatabaseContext GetDefaultContext(EntityFramework.DbContextScope.Interfaces.IDbContextReadOnlyScope dbContextScope)
        {
            return GetContext<GameDatabaseContext>(dbContextScope);
        }

        protected override BaseDatabaseContext GetDefaultContext(EntityFramework.DbContextScope.Interfaces.IDbContextScope dbContextScope)
        {
            return GetContext<GameDatabaseContext>(dbContextScope);
        }

        protected override void OnCreateAccount(BaseDatabaseContext databaseContext, DbAccount account)
        {
            var db = databaseContext as GameDatabaseContext;
            var gameData = new GameData();
            gameData.Account = account;
            gameData.Characters = new List<Character>();
            db.GameDatas.Add(gameData);
        }

        class AccountDeleteDescription : IDeletingAccountDescription
        {
            public DbAccount Account { get; set; }
            public GameData GameData { get; set; }
            public List<Character> Characters { get; set; }
        }

        protected override IQueryable<IDeletingAccountDescription> CreateQueryForAccountDeletion(int accountId, BaseDatabaseContext dbContext)
        {
            var db = dbContext as GameDatabaseContext;

            var query = (from acc in db.Accounts
                         from data in db.GameDatas
                         where acc.Id == accountId
                         where data.Account.Id == accountId
                         select new AccountDeleteDescription  { Account = acc, GameData = data, Characters = data.Characters });

            return query;
        }

        protected override void OnDeleteAccount(IDeletingAccountDescription deleteDescription, BaseDatabaseContext dbContext)
        {
            var desc = deleteDescription as AccountDeleteDescription;
            var db = dbContext as GameDatabaseContext;

            db.GameDatas.Remove(desc.GameData);

            var characters = desc.Characters;

            foreach (var c in characters)
            {
                db.Characters.Remove(c);
            }
        }

        public async Task<bool> HasCharacterWithName(string name)
        {
            using (var contextScope = ContextFactory.CreateReadOnly())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var characterQuery =
                    from character in db.Characters
                    where character.Name == name
                    select character;

                return await characterQuery.AnyAsync();
            }
        }

        // WRITE
        public async Task<Db_CreateCharacterResult> CreateCharacterAsync(AccountId id, Character newCharacter, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.CreateWithTransaction(System.Data.IsolationLevel.Unspecified))
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var gameDataQuery = from data in db.GameDatas where data.Account.Id == id.Value select data;
                gameDataQuery = gameDataQuery.Include((x) => x.Characters);

                var gameData = await gameDataQuery.FirstOrDefaultAsync();

                if (gameData == null)
                {
                    return new Db_CreateCharacterResult { Result = DatabaseResultTypes.NoData };
                }

                if(gameData.Characters.Count >= 10)
                {
                    return new Db_CreateCharacterResult { Result = DatabaseResultTypes.TooMany };
                }

                foreach (var character in gameData.Characters)
                {
                    if (character.Name == newCharacter.Name)
                    {
                        return new Db_CreateCharacterResult { Result = DatabaseResultTypes.AlreadyCreated };
                    }
                }

                if (gameData.Characters.Count == 0)
                {
                    gameData.SelectedCharacter = newCharacter;
                }

                gameData.Characters.Add(newCharacter);

                db.GameDatas.Update(gameData);

                await contextScope.SaveChangesAsync();

                return new Db_CreateCharacterResult { Result = DatabaseResultTypes.Success, Character = newCharacter };
            }
        }

        // WRITE
        public async Task<bool> DeleteCharacterAsync(AccountId id, string characterName, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.CreateWithTransaction(System.Data.IsolationLevel.Unspecified))
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var gameDataQuery = from data in db.GameDatas where data.Account.Id == id.Value select data;
                gameDataQuery = gameDataQuery.Include((x) => x.Characters);

                var gameData = await gameDataQuery.FirstOrDefaultAsync();

                if (gameData == null)
                {
                    return false;
                }

                for (int i = 0; i < gameData.Characters.Count; i++)
                {
                    Character character = gameData.Characters[i];
                    if (character.Name == characterName)
                    {
                        gameData.Characters.Remove(character);
                        db.GameDatas.Update(gameData);
                        await contextScope.SaveChangesAsync();
                        return true;
                    }
                }
                
                return false;
            }
        }

        // WRITE
        public async Task SelectCharacterAsync(AccountId id, string characterName, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.Create())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var dataResult = await (from data in db.GameDatas
                                      
                                      where data.Account.Id == id.Value
                                      select new { data, data.SelectedCharacter, data.Characters }).FirstOrDefaultAsync(token);

                if (dataResult == null || dataResult.data == null)
                {
                    log.Error($"No gamedata found for account id {id.Value}");
                    return;
                }

                var gameData = dataResult.data;

                if (gameData.SelectedCharacter != null && gameData.SelectedCharacter.Name == characterName)
                {
                    log.Warn($"character {characterName} already selected for account {id.Value}");
                    return;
                }

                Character targetCharacter = null;

                foreach(var character in gameData.Characters)
                {
                    if(character.Name == characterName)
                    {
                        targetCharacter = character;
                        break;
                    }
                }

                if(targetCharacter == null)
                {
                    log.Error($"No character found with id {characterName} for accound {id.Value}");
                    return;
                }

                gameData.SelectedCharacter = targetCharacter;

                db.GameDatas.Update(gameData);

                await contextScope.SaveChangesAsync();
            }
        }


        public async Task<Character> GetSelectedCharacterAsync(AccountId id, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.CreateReadOnly())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var characterQuery =
                    from gameData in db.GameDatas
                    where id.Value == gameData.Account.Id
                    select new { gameData.SelectedCharacter };

                //characterQuery = characterQuery.Include(c => c.Items);
                var result = await characterQuery.FirstOrDefaultAsync(token);

                return result.SelectedCharacter;
            }
        }

        public async Task<Character[]> GetAllCharactersAsync(AccountId id, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.CreateReadOnly())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);

                var characterQuery = db.GameDatas.Where(gameData => gameData.Account.Id == id.Value).SelectMany(gameData => gameData.Characters);
                
                return await characterQuery.ToArrayAsync(token);
            }
        }

        public async Task<GameData> GetGameDataByIdAsync(AccountId id, bool includeAccount, bool includeCharacters, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.CreateReadOnly())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);
                IQueryable<GameData> query = db.GameDatas;

                if (includeAccount)
                {
                    query = query.Include(data => data.Account);
                }

                if (includeCharacters)
                {
                    query = query.Include(data => data.Characters);
                }

                query =
                (from gameData in query
                 where id.Value == gameData.Account.Id
                 select gameData);

                try
                {
                    var result = await query.FirstOrDefaultAsync(token);
                    return result;
                }
                catch (System.Exception ex)
                {
                    log.Error(ex.Message);
                    return null;
                }
            }
        }

        public async Task SaveCharactersAsync(Character[] characters, CancellationToken token = default)
        {
            using (var contextScope = ContextFactory.Create())
            {
                var db = GetContext<GameDatabaseContext>(contextScope);
                db.Characters.UpdateRange(characters);
                await contextScope.SaveChangesAsync(token).ConfigureAwait(false);
            }
        }
    }
}
