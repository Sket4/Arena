using System;
using Grpc.Core;
using Arena.Server;
using TzarGames.MatchFramework.Database;
using TzarGames.MatchFramework.Database.Server;
using Microsoft.Extensions.Configuration;
using TzarGames.MatchFramework.Server;
using NLog;

namespace DatabaseApp
{
    class AppConfiguration
    {
        public int DatabaseServerPort { get; set; }
        public string DatabaseServiceCertificateFilePath { get; set; }
        public string DatabaseServicePrivateKeyFilePath { get; set; }
        public DatabaseConnectionSettings DatabaseConnectionSettings { get; set; }
    }

    class Program
    {
        static AppConfiguration Config { get; set; }
        static Logger log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            processArgs(args);

            setupConfiguration();

            var db = new DB.GameDatabase(false, Config.DatabaseConnectionSettings);

            var credentials = loadCredentials();

            if (credentials == null)
            {
                return;
            }

            var server = new Server()
            {
                Services =
                    {
                        DatabaseService.BindService(new DatabaseServiceImpl(db)),
                        GameDatabaseService.BindService(new GameDatabaseServiceImpl(db))
                    },
                Ports = { new ServerPort("0.0.0.0", Config.DatabaseServerPort, credentials) }
            };

            server.Start();

            log.Info("Database server listening on port " + Config.DatabaseServerPort);

            ServerUtility.RunCommandLoop();

            server.ShutdownAsync().Wait();
            log.Info("Database server closed");
        }

        private static void processArgs(string[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-debug_grpc":
                        {
                            log.Info("GRPC debug enabled");
                            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
                        }
                        break;
                }
            }
        }

        static ServerCredentials loadCredentials()
        {
            string certificate;

            try
            {
                certificate = System.IO.File.ReadAllText(Config.DatabaseServiceCertificateFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read certificate file from path " + Config.DatabaseServiceCertificateFilePath);
                return null;
            }

            string key;

            try
            {
                key = System.IO.File.ReadAllText(Config.DatabaseServicePrivateKeyFilePath);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to read private key from path " + Config.DatabaseServicePrivateKeyFilePath);
                return null;
            }

            var certKeyPair = new KeyCertificatePair(certificate, key);
            return new SslServerCredentials(new[] { certKeyPair });
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
