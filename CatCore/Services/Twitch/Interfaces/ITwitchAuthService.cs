using System.Threading.Tasks;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchAuthService : INeedAsyncInitialization
	{
		string? AccessToken { get; }
		bool IsValid { get; }

		ValidationResponse? LoggedInUser { get; }

		string AuthorizationUrl(string redirectUrl);
		Task<AuthorizationResponse?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl);
		Task<ValidationResponse?> ValidateAccessToken();
		Task<bool> RefreshTokens();
		Task<bool> RevokeTokens();
	}
}