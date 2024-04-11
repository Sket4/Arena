using System.Threading.Tasks;
using Arena;
using TzarGames.MatchFramework.Client;

namespace Arena.Client
{
    public class ClientAuthenticationService : IAuthenticationService
    {
        public string AuthenticationToken { get; set; } = "0";
        public bool DebugMode { get; set; }

        public async Task<bool> Authenticate()
        {
            if (DebugMode)
                return false;

            if (GameState.Instance == null)
            {
                return false;
            }

            AuthenticationToken = await Authentication.AuthenticateUsingFirebaseToken(GameState.Instance.AuthenticationToken, GameState.Instance.AuthServerCertificate);
            return true;
        }
    }
}
