using Grpc.Core;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Arena.Server
{
    public class ServerGameManager : GameServerManager
    {
        [SerializeField]
        bool disableJobs = true;

        [SerializeField]
        ServerAddress dbServerAddress = new ServerAddress("127.0.0.1", 50052);

        [SerializeField]
        ServerAddress authServerAddress = new ServerAddress("127.0.0.1", 60700);

        [SerializeField] ServerGameSettings gameSettings;

        Channel channel;
        ServerAuthService.ServerAuthServiceClient authClient;
        AuthService authService;

        protected override GameServerLoopBase CreateGameServerLoop(IServerGameSettings serverSettings, Unity.Entities.Hash128[] additionalScenes)
        {
            return new GameServerLoop(serverSettings, additionalScenes, authService, dbServerAddress);
        }

        void initArgs()
        {
            var args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                switch(arg) 
                {
                    case "-databaseserveraddress":
                        dbServerAddress.Address = args[i + 1];
                        Debug.Log($"Database server ip changed to {dbServerAddress.Address}");
                        break;

                    case "-databaseserverport":
                        dbServerAddress.Port = ushort.Parse(args[i + 1]);
                        Debug.Log($"Database server port changed to {dbServerAddress.Port}");
                        break;

                    case "-authserveraddress":
                        authServerAddress.Address = args[i+1];
                        Debug.Log($"Auth server address changed to {authServerAddress.Address}");
                        break;

                    case "-authserverport":
                        authServerAddress.Port = ushort.Parse(args[i + 1]);
                        Debug.Log($"Auth server port changed to {authServerAddress.Port}");
                        break;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if(channel != null)
            {
                var _ = channel.ShutdownAsync();
            }
        }

        private void Awake()
        {
//#if !UNITY_EDITOR
//            Debug.Log("Disabling stack trace logging");
//            //Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
//            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
//            //Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
//            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
//            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
//#endif

            if (disableJobs)
            {
                Debug.Log("Disabling jobs");
                JobsUtility.JobWorkerCount = 0;
            }

#if !UNITY_EDITOR
            Debug.Log("Disabling physics auto simulation");
            Physics.autoSimulation = false;
#endif

            DontDestroyOnLoad(gameObject);
            initArgs();

            ChannelCredentials credentials = new SslCredentials(authServerAddress.Certificate.text);

            channel = new Channel(authServerAddress.Address, (int)authServerAddress.Port, credentials);
            authClient = new ServerAuthService.ServerAuthServiceClient(channel);

            authService = new AuthService(authClient);
        }

#if UNITY_EDITOR
        public void LaunchServer(string serverName, int maxConnections, bool useSimulator, SimParameters simParameters,  out ushort port, GameSessionInfo gameSessionInfo)
        {
            var game = CreateServer(gameSettings);
            port = game.Port;

            var matchSystem = (game as GameServerLoop).World.GetExistingSystemManaged<ArenaMatchSystem>();
            _ = matchSystem.CreateGameSessionAsync(gameSessionInfo);
        }
#endif
    }
}

