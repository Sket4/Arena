using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Grpc.Core;
using TzarGames.FallGame.Client.Tests;

namespace TestServer
{
    class TestServiceImpl : TestService.TestServiceBase
    {
        public override async Task<Result> GetToken(TokenRequest request, ServerCallContext context)
        {
            var token = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(request.UserId);
            var result = new Result
            {
                AuthToken = token
            };
            return result;
        }

        public override async Task<PingResult> Ping(PingRequest request, ServerCallContext context)
        {
            var sw = new Stopwatch();
            sw.Start();
            await Task.Delay(2000);
            Console.WriteLine("PingTest seconds: {0}", (sw.ElapsedMilliseconds / 1000.0f));
            return new PingResult();
        }
    }
}
