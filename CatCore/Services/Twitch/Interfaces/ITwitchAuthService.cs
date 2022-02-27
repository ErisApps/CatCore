using System;
using System.Threading.Tasks;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchAuthService
	{
		string? AccessToken { get; }
		bool HasTokens { get; }
		bool TokenIsValid { get; }

		AuthenticationStatus Status { get; }
		event Action? OnCredentialsChanged;

		ValidationResponse? FetchLoggedInUserInfo();
		Task<ValidationResponse?> FetchLoggedInUserInfoWithRefresh();

		string AuthorizationUrl(string redirectUrl);
		Task GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl);
		Task<ValidationResponse?> ValidateAccessToken(bool resetDataOnFailure = true);
		Task<bool> RefreshTokens();
		Task<bool> RevokeTokens();
	}
}