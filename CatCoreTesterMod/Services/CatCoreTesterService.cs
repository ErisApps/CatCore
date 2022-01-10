using System.Linq;
using CatCore;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using Zenject;

namespace CatCoreTesterMod.Services
{
	internal class CatCoreTesterService : IInitializable
	{
		private readonly SiraLog _logger;
		private readonly CatCoreInstance _chatCoreInstance;

		public CatCoreTesterService(SiraLog logger, UBinder<Plugin, CatCoreInstance> chatCoreInstance)
		{
			_logger = logger;
			_chatCoreInstance = chatCoreInstance.Value;
		}

		public async void Initialize()
		{
			var userInfo = await _chatCoreInstance.RunTwitchServices().GetHelixApiService().FetchUserInfo(loginNames: new[] { "realeris" }).ConfigureAwait(false);
			if (userInfo != null)
			{
				var erisProfileData = userInfo.Value.Data.First();
				_logger.Info($"Successfully requested info for user {erisProfileData.DisplayName}");
			}
			else
			{
				_logger.Warn("Something went wrong while trying to fetch the info of the requested user.");
			}
		}
	}
}