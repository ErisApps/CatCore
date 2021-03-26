using CatCore;
using CatCoreTesterMod.Services;
using IPA.Logging;
using SiraUtil;
using SiraUtil.Zenject;
using Zenject;

namespace CatCoreTesterMod.Installers
{
	internal class CatCoreInstaller : Installer<Logger, ChatCoreInstance, CatCoreInstaller>
	{
		private readonly Logger _logger;
		private readonly ChatCoreInstance _chatCoreInstance;

		internal CatCoreInstaller(Logger logger, ChatCoreInstance chatCoreInstance)
		{
			_logger = logger;
			_chatCoreInstance = chatCoreInstance;
		}

		public override void InstallBindings()
		{
			Container.BindLoggerAsSiraLogger(_logger);
			Container.BindInstance(new UBinder<Plugin, ChatCoreInstance>(_chatCoreInstance)).AsSingle();
			Container.BindInterfacesAndSelfTo<CatCoreTesterService>().AsSingle();
		}
	}
}