using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchAuthService : INeedAsyncInitialization
	{
		string? AccessToken { get; }
		bool HasTokens { get; }
		bool TokenIsValid { get; }

		event Action? OnCredentialsChanged;

		ValidationResponse? LoggedInUser { get; }

		string AuthorizationUrl(string redirectUrl);
		Task GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl);
		Task<ValidationResponse?> ValidateAccessToken(bool resetDataOnFailure = true);
		Task<bool> RefreshTokens();
		Task<bool> RevokeTokens();
	}
}