using Arena.Server;
using Grpc.Core;

namespace FrontendApp
{
    public static class Utils
    {
        public static GameDatabaseService.GameDatabaseServiceClient CreateDatabaseClient(string ip, int port, string databaseCertificate)
        {
            ChannelCredentials credentials;

            if (string.IsNullOrEmpty(databaseCertificate))
            {
                credentials = ChannelCredentials.Insecure;
            }
            else
            {
                credentials = new SslCredentials(databaseCertificate);
            }

            var channel = new Channel(ip, port, credentials);
            return new GameDatabaseService.GameDatabaseServiceClient(channel);
        }
    }
}
