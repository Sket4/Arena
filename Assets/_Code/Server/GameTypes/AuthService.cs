using System.Threading.Tasks;
using TzarGames.MatchFramework;
using TzarGames.MatchFramework.Server;

namespace Arena.Server
{
    public class AuthService : IAuthorizationService
    {
        ServerAuthService.ServerAuthServiceClient authService;

        public AuthService(ServerAuthService.ServerAuthServiceClient authService)
        {
            this.authService = authService;
        }

        public async Task<AuthorizationResult> AuthorizeByUserToken(string token)
        {
            UnityEngine.Debug.Log($"Авторизация пользователя по токену {token}");
            var authResult = await authService.GetAccountDataFromTokenAsync(new AccountDataRequest { AuthToken = token });
            if (authResult.State != AccountDataResult.Types.AccountDataState.Success)
            {
                var authState = AuthorizationResultState.UnknownError;
                if (authResult.State == AccountDataResult.Types.AccountDataState.TokenExpired)
                {
                    authState = AuthorizationResultState.TokenExpired;
                }
                return new AuthorizationResult(new PlayerId(authResult.Id.Value), authState);
            }
            return new AuthorizationResult(new PlayerId(authResult.Id.Value), AuthorizationResultState.Success);
        }
    }
}
