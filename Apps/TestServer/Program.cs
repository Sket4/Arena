using System;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Grpc.Core;
using TzarGames.FallGame.Client.Tests;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault(),
                });

                Console.WriteLine("Test service started");

                var clientServer = new Server()
                {
                    Services =
                    {
                        TestService.BindService(new TestServiceImpl())
                    },
                    Ports = { new ServerPort("localhost", TestLib.TestUtility.TestServerPort, ServerCredentials.Insecure) }
                };

                clientServer.Start();
                Console.WriteLine("Test server listening on port " + TestLib.TestUtility.TestServerPort);

                Console.WriteLine("Press any key to stop the server");
                Console.ReadKey();

                clientServer.ShutdownAsync().Wait();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
