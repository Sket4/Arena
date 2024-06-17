using Grpc.Core;
using TzarGames.MatchFramework;
using static TzarGames.MatchFramework.Client.ClientUtility;

namespace Arena.Client
{
    public static class ClientGameUtility
    {
        public const int DefaultFrontendPort = 60500;

        public static ServiceWrapper<ArenaClientService.ArenaClientServiceClient> GetGameClient(IAuthTokenProvider authTokenProvider, string frontendIp, int frontendPort, string certificate)
        {
            var credentials = new SslCredentials(certificate);
            return new ServiceWrapper<ArenaClientService.ArenaClientServiceClient>(frontendIp, frontendPort,
                credentials, (channel) => SetupGameClientChannel(channel, authTokenProvider));
        }
    }
}
