using System.Threading.Tasks;
using CatCore.Models.Twitch.OAuth;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchAuthService
	{
		string AuthorizationUrl(string redirectUrl);
		Task<AuthorizationResponse?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl);
		Task<ValidationResponse?> ValidateAccessToken();
		Task<bool> RefreshTokens();
		Task<bool> RevokeToken();
	}
}