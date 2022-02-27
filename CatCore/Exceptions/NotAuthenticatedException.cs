using System;
using CatCore.Models.Shared;

namespace CatCore.Exceptions
{
	public abstract class NotAuthenticatedException : Exception
	{
		private readonly PlatformType _platform;
		public override string Message => $"Non valid credentials are present for platform {_platform:G}, make sure the user is logged in or try again later.";

		protected NotAuthenticatedException(PlatformType platform)
		{
			_platform = platform;
		}
	}

	public class TwitchNotAuthenticatedException : NotAuthenticatedException
	{
		public TwitchNotAuthenticatedException() : base(PlatformType.Twitch)
		{
		}
	}
}