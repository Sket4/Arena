using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using Microsoft.Extensions.Configuration;
using Arena.Client;
using TzarGames.MatchFramework.Frontend.Server;
using TzarGames.MatchFramework.Server;
using NLog;

namespace FrontendApp
{
    class AppConfiguration
    {
        public int FrontendPort { get; set; }
        public string ServerAuthIP { get; set; }
        public int ServerAuthPort { get; set; }
        public string DatabaseServerIP { get; set; }
        public int DatabaseServerPort { get; set; }
        public int InternalFrontendPort { get; set; }
        public string ClientServiceCertChainFilePath { get; set; }
        public string ClientServicePrivateKeyFilePath { get; set; }
        public string InternalAuthServerCertificateFilePath { get; set; }
        public string DatabaseCertificateFilePath { get; set; }
        public string InternalServicePrivateKeyFilePath { get; set; }
        public string InternalServiceCertChainFilePath { get; set; }
        public string GameServiceCertificateFilePath { get; set; }
    }

    class Program
    {
        static AppConfiguration Config { get; set; }
        static InternalFrontendServiceImpl internalService;
        static Server clientServer;
        static Server internalServer;
        static Logger log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            setupConfiguration();
            
            var dbCertificate = loadDatabaseCertificate();

            var clientService = launchClientServer(dbCertificate);

            var gameServiceCertificate = loadGameServiceCertificate();

            launchInternalServer(dbCertificate, gameServiceCertificate, new IMessageQueuePushable<IMessage>[] { clientService.IncomingMessages });

            ServerUtility.AddCustomCommandToLoop("loggamerooms", (val) =>
            {
                clientService.LogGameRoomInfo();
            });

            ServerUtility.AddCustomCommandToLoop("logsaferooms", (val) =>
            {
                clientService.LogSafeAreaRoomInfo();
            });

            ServerUtility.RunCommandLoop();

            if(internalService != null)
            {
                internalService.Stop();
            }

            clientServer.ShutdownAsync().Wait();
            internalServer.ShutdownAsync().Wait();
        }

        private static string loadDatabaseCertificate()
        {
            try
            {
                return System.IO.File.ReadAllText(Config.DatabaseCertificateFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read database certificate from path " + Config.DatabaseCertificateFilePath);
                throw ex;
            }
        }

        static ArenaClientGameServiceImpl launchClientServer(string dbCertificate)
        {
            log.Info("Starting client server...");

            string internalAuthCertificate;
            try
            {
                internalAuthCertificate = System.IO.File.ReadAllText(Config.InternalAuthServerCertificateFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read internal auth server certificate file at path " + Config.InternalAuthServerCertificateFilePath);
                throw ex;
            }

            var authInterceptor = new ServerAuthInterceptor(Config.ServerAuthIP, Config.ServerAuthPort, internalAuthCertificate);

            string certificate;
            try
            {
                certificate = System.IO.File.ReadAllText(Config.ClientServiceCertChainFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read client service certificate file at path " + Config.ClientServiceCertChainFilePath);
                throw ex;
            }

            string privateKey;
            try
            {
                privateKey = System.IO.File.ReadAllText(Config.ClientServicePrivateKeyFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read client service private key file at path " + Config.ClientServicePrivateKeyFilePath);
                throw ex;
            }

            var keyCertPair = new KeyCertificatePair(certificate, privateKey);
            var credentials = new SslServerCredentials(new[] { keyCertPair });

            //var gameLogic = new ServerGameLogic(Config.DatabaseServerIP, Config.DatabaseServerPort);

            var service = new ArenaClientGameServiceImpl(Config.DatabaseServerIP, Config.DatabaseServerPort, dbCertificate);

            clientServer = new Server()
            {
                Services =
                {
                    ArenaClientService.BindService(service).Intercept(authInterceptor)
                },
                Ports = { new ServerPort("0.0.0.0", Config.FrontendPort, credentials) }
            };
            clientServer.Start();

            log.Info($"Client server started, port {Config.FrontendPort}");
            return service;
        }

        static string loadGameServiceCertificate()
        {
            try
            {
                return System.IO.File.ReadAllText(Config.GameServiceCertificateFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read game service certificate from path " + Config.GameServiceCertificateFilePath);
                throw ex;
            }
        }

        static void launchInternalServer(string dbCertificate, string gameServiceCertificate, IMessageQueuePushable<IMessage>[] internalServerMessageListeners)
        {
            log.Info("Starting internal server...");

            string internalServiceCertChain;
            try
            {
                internalServiceCertChain = System.IO.File.ReadAllText(Config.InternalServiceCertChainFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read internal service cert chain file at path " + Config.InternalServiceCertChainFilePath);
                throw ex;
            }

            string internalServicePrivateKey;
            try
            {
                internalServicePrivateKey = System.IO.File.ReadAllText(Config.InternalServicePrivateKeyFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read internal service private key file at path " + Config.InternalServicePrivateKeyFilePath);
                throw ex;
            }

            var internalServiceKeyCertPair = new KeyCertificatePair(internalServiceCertChain, internalServicePrivateKey);
            var internalServiceCreds = new SslServerCredentials(new[] { internalServiceKeyCertPair });

            internalService = new InternalFrontendServiceImpl(gameServiceCertificate, internalServerMessageListeners);
            //var internalGameService = new InternalFrontendGameServiceImpl(Config.DatabaseServerIP, Config.DatabaseServerPort, dbCertificate);

            internalServer = new Server()
            {
                Services =
                {
                    InternalFrontendService.BindService(internalService),
                    //InternalFrontendGameService.BindService(internalGameService)
                },
                Ports = { new ServerPort("0.0.0.0", Config.InternalFrontendPort, internalServiceCreds) }
            };
            internalServer.Start();

            log.Info($"Internal server started, port {Config.InternalFrontendPort}");
        }

        static void setupConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("settings.json");
            var configuration = builder.Build();
            Config = new AppConfiguration();
            var section = configuration.GetSection("Configuration");
            Config = section.Get<AppConfiguration>();
        }
    }
}
