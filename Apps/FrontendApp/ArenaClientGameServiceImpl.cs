using Arena;
using Arena.Client;
using Arena.Server;
using Grpc.Core;
using NLog;
using System;
using System.Threading.Tasks;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Database;
using TzarGames.MatchFramework.Frontend.Server;

namespace FrontendApp
{
    class ArenaClientGameServiceImpl : ArenaClientService.ArenaClientServiceBase, IMessageQueuePushable<IMessage>
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        // разделил апдейтеры для возможности параллельного подключения к двум комнатам
        MatchmakingUpdateReactor safeAreaMatchmakingReactorUpdater;
        MatchmakingUpdateReactor matchmakingReactorUpdater;
        GameDatabaseService.GameDatabaseServiceClient dbClient;

        public void PushMessage(IMessage message)
        {
            safeAreaMatchmakingReactorUpdater.IncomingMessages.PushMessage(message);
            matchmakingReactorUpdater.IncomingMessages.PushMessage(message);
        }

        public IMessageQueuePushable<IMessage> IncomingMessages => this;
        ServerGameLogic logic;

        public ArenaClientGameServiceImpl(string databaseServerIp, int databaseServerPort, string databaseCertificate)
        {
            matchmakingReactorUpdater = new MatchmakingUpdateReactor();
            safeAreaMatchmakingReactorUpdater = new MatchmakingUpdateReactor();

            dbClient = Utils.CreateDatabaseClient(databaseServerIp, databaseServerPort, databaseCertificate);
            logic = new ServerGameLogic(dbClient);
        }

        ~ArenaClientGameServiceImpl()
        {
            matchmakingReactorUpdater.Dispose();
            safeAreaMatchmakingReactorUpdater.Dispose();
        }

        public override async Task<Arena.Client.GetCharactersResult> GetCharacters(GetCharactersRequest request, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            try
            {
                var result = await dbClient.GetCharactersForAccountAsync(new GetCharacterRequest() 
                { 
                    AccountId = userId,
                    
                });

                if (result == null)
                {
                    throw new Exception("failed to get charactes for account");
                }

                var clientResult = new Arena.Client.GetCharactersResult();
                foreach (var character in result.Characters)
                {
                    character.ID = 0;
                    clientResult.Characters.Add(character);
                }

                return clientResult;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        public override async Task<Arena.Client.CreateCharacterResult> CreateCharacter(CreateCharacterRequest request, ServerCallContext context)
        {
            if (request.Gender != Genders.Male || request.Gender != Genders.Female)
            {
                log.Error("invalied gender");
                return null;
            }
            if(request.EyeColor < 0 || Identifiers.EyeColors.Length <= request.EyeColor)
            {
                log.Error("invalid eye color");
                return null;
            }
            if(request.HairColor < 0 || Identifiers.HairColors.Length <= request.HairColor)
            {
                log.Error("invalid hair color");
                return null;
            }
            if(request.SkinColor < 0 || Identifiers.SkinColors.Length <= request.SkinColor)
            {
                log.Error("invalid skin color");
                return null;
            }

            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            var createRequest = new CharacterCreateRequest();
            createRequest.AccountId = userId;
            createRequest.Class = request.Class;
            createRequest.Name = request.Name;
            createRequest.Gender = request.Gender;
            createRequest.HeadID = request.HeadID;
            createRequest.HairstyleID = request.HairstyleID;
            createRequest.EyeColor = request.EyeColor;
            createRequest.SkinColor = request.SkinColor;
            createRequest.HairColor = request.HairColor;
            createRequest.ArmorColor = request.ArmorColor;

            var result = await dbClient.CreateCharacterForAccountAsync(createRequest);

            return new Arena.Client.CreateCharacterResult() { Character = result.Character, ErrorMessage = result.Result.ToString(), Success = result.Result == DatabaseResultTypes.Success };
        }

        public override async Task<DeleteCharacterResult> DeleteCharacter(DeleteCharacterRequest request, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            var result = await dbClient.DeleteCharacterForAccountAsync(new CharacterDeleteRequest
            {
                AccountId = userId,
                CharacterName = request.Name
            });

            return new DeleteCharacterResult
            {
                Success = result.Success
            };
        }


        public override async Task<SelectCharacterResult> SelectCharacter(SelectCharacterRequest request, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            var selectRequest = new DbSelectCharacterRequest();
            selectRequest.AccountId = userId;
            selectRequest.CharacterName = request.Name;

            var result = await dbClient.SelectCharacterForAccountAsync(selectRequest);

            return new SelectCharacterResult() { Success = true };
        }

        public override async Task<GetSelectedCharacterResult> GetSelectedCharacter(GetSelectedCharacterRequest request, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            var getRequest = new GetCharacterRequest();
            getRequest.AccountId = userId;

            var result = await dbClient.GetSelectedCharacterForAccountAsync(getRequest);
            return new GetSelectedCharacterResult() { Character = result.Character };
        }

        public override async Task<GetGameDataResult> GetGameData(GetGameDataRequest request, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            var result = await dbClient.GetGameDataForAccountAsync(new GetGameDataForAccountRequest { AccountId = userId });

            return new GetGameDataResult
            {
                Data = result.Data
            };
        }

        public class ArenaRoomMessageHandler : RoomMessageHandler
        {
            protected ServerGameLogic gameLogic;

            public ArenaRoomMessageHandler(AccountId userId, MatchmakingUpdateReactor marchmakingReactor, ServerGameLogic serverGameLogic) : base(userId, marchmakingReactor)
            {
                gameLogic = serverGameLogic;
            }

            protected override async Task<ServerRoomMessage> OnReceiveMessageFromRoom(ServerToClientMessage message)
            {
                log.Info($"OnReceiveMessageFromRoom {message.MetaData?.DumpDataToString()}");

                if (message is GameResultMessage gameResultMessage)
                {
                    gameResultMessage.Message.SetAsEndMessage();
                    return gameResultMessage.Message;
                }

                return await base.OnReceiveMessageFromRoom(message);
            }
        }

        class SafeAreaRoomMessageHandler : ArenaRoomMessageHandler
        {
            public SafeAreaRoomMessageHandler(AccountId userId, MatchmakingUpdateReactor marchmakingReactor, ServerGameLogic serverGameLogic) : base(userId, marchmakingReactor, serverGameLogic)
            {
            }

            protected override Task<ClientRequestMessage> OnReceiveRequestFromClient(ClientRoomMessage clientMessage)
            {
                log.Info($"OnReceiveRequestFromClient {clientMessage.MetaData?.DumpDataToString()}");

                if (clientMessage.IsCreateGameRequest())
                {
                    if (clientMessage.MetaData.IntKeyValues.TryGet(MetaDataKeys.SceneId, out var sceneId) == false)
                    {
                        log.Error($"no scene id in client game request, client id {UserId.Value}");
                        return null;
                    }

                    var roomMetaData = new MetaData();
                    roomMetaData.IntKeyValues.Add(MetaDataKeys.SceneId, sceneId);

                    if (Arena.SharedUtility.IsSafeZoneLocation(sceneId))
                    {
                        log.Info($"requesting meetplace game server for user {UserId.Value}");

                        var roomFlags = RoomFlags.CanAddUsersToRunningGameServer | RoomFlags.StartGameServerWhenReachedMinimumPlayerCount;
                        var result = new MultiplayerClientGameRequestMessage(UserId, clientMessage.GetGameType(), 1, 20, roomMetaData, clientMessage.MetaData, roomFlags);
                        return Task.FromResult(result as ClientRequestMessage);
                    }
                    else
                    {
                        log.Error($"invalid scene id {sceneId} for safe area, client id {UserId.Value}");
                        return null;
                    }
                }

                return base.OnReceiveRequestFromClient(clientMessage);
            }
        }

        class GameRoomMessageHandler : ArenaRoomMessageHandler
        {
            public GameRoomMessageHandler(AccountId userId, MatchmakingUpdateReactor marchmakingReactor, ServerGameLogic serverGameLogic) : base(userId, marchmakingReactor, serverGameLogic)
            {
            }

            protected override Task<ClientRequestMessage> OnReceiveRequestFromClient(ClientRoomMessage clientMessage)
            {
                log.Info($"OnReceiveRequestFromClient {clientMessage.MetaData?.DumpDataToString()}");

                if (clientMessage.IsCreateGameRequest())
                {
                    if (clientMessage.MetaData.IntKeyValues.TryGet(MetaDataKeys.SceneId, out var sceneId) == false)
                    {
                        log.Error($"no scene id in client game request, client id {UserId.Value}");
                        return null;
                    }

                    var roomMetaData = new MetaData();
                    roomMetaData.IntKeyValues.Add(MetaDataKeys.SceneId, sceneId);

                    if (Arena.SharedUtility.IsSafeZoneLocation(sceneId) == false)
                    {
                        if(clientMessage.MetaData.BoolKeyValues.TryGet(MetaDataKeys.MultiplayerGame, out var isMultiplayer))
                        {
                            log.Info($"requesting multiplayer game server for user {UserId.Value}");

                            var result = new MultiplayerClientGameRequestMessage(UserId, clientMessage.GetGameType(), 2, 8, roomMetaData, clientMessage.MetaData);
                            return Task.FromResult(result as ClientRequestMessage);
                        }
                        else
                        {
                            log.Info($"requesting singleplayer game server for user {UserId.Value}");

                            var result = new SinglePlayerGameRequestMessage(UserId, clientMessage.GetGameType(), roomMetaData, clientMessage.MetaData);
                            return Task.FromResult(result as ClientRequestMessage);
                        }
                    }
                    else
                    {
                        log.Error($"invalid scene id {sceneId} for game, client id {UserId.Value}");
                        return null;
                    }
                }

                return base.OnReceiveRequestFromClient(clientMessage);
            }
        }

        public override async Task GameRoomConnection(IAsyncStreamReader<ClientRoomMessage> requestStream, IServerStreamWriter<ServerRoomMessage> responseStream, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            log.Info($"Game room connection for user {userId.Value}");

            var roomMessageReactor = new GameRoomMessageHandler(userId, matchmakingReactorUpdater, logic);

            await roomMessageReactor.StartHandlingMessages(requestStream, responseStream, context.CancellationToken);
            log.Info($"Finished game update loop of room message reactor for user {userId.Value}");
        }

        public override async Task SafeAreaRoomConnection(IAsyncStreamReader<ClientRoomMessage> requestStream, IServerStreamWriter<ServerRoomMessage> responseStream, ServerCallContext context)
        {
            var userId = TzarGames.MatchFramework.Frontend.Server.Utils.CheckUserIdAndThrowExceptionWhenFailed(context);

            log.Info($"Safe area room connection for user {userId.Value}");

            var roomMessageReactor = new SafeAreaRoomMessageHandler(userId, safeAreaMatchmakingReactorUpdater, logic);

            await roomMessageReactor.StartHandlingMessages(requestStream, responseStream, context.CancellationToken);
            log.Info($"Finished safe area update loop of room message reactor for user {userId.Value}");
        }

        public void LogGameRoomInfo()
        {
            log.Info("Game matchmaking reactor:");
            matchmakingReactorUpdater.LogInfo();
        }
        public void LogSafeAreaRoomInfo()
        {
            log.Info("Safe area matchmaking reactor:");
            safeAreaMatchmakingReactorUpdater.LogInfo();
        }
    }
}
