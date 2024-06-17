using Grpc.Core;
using Arena.Client.Tests;

namespace TestLib
{
    public static class TestUtility
    {
        public const int TestServerPort = 62601;

        public static TestService.TestServiceClient GetTestClient(string ip = "127.0.0.1")
        {
            var channel = new Channel(ip, 62601, ChannelCredentials.Insecure);
            var client = new TestService.TestServiceClient(channel);
            return client;
        }
    }
}
