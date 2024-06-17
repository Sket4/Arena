using Grpc.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServerLib;
using TzarGames.MatchFramework.Server;

namespace TesterApp
{
    [TestFixture]
    class Tests
    {
        [Test]
        public static void testCharacterDb()
        {
            var accountClient = DatabaseLib.DatabaseUtility.CreateDatabaseClient();

            var createAccRequest = new CreateAccountRequest();
            var createAccResult = accountClient.CreateAccount(createAccRequest);

            var gameClient = DatabaseGameLib.DatabaseUtility.CreateDatabaseClient();
            var createCharRequest = new TzarGames.FallGame.Server.CharacterCreateRequest();
            createCharRequest.AccountId = createAccResult.Account.Id;
            gameClient.CreateCharacterForAccount(createCharRequest);

            var getCharRequest = new TzarGames.FallGame.Server.GetCharacterRequest();
            getCharRequest.AccountId = createAccResult.Account.Id;
            var result = gameClient.GetSelectedCharacterForAccount(getCharRequest);

            Console.WriteLine("Character class {0} xp {1}", result.Character.Class, result.Character.XP);
        }
    }
}
