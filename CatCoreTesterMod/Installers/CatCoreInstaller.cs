using CatCore;
using CatCoreTesterMod.Services;
using IPA.Logging;
using SiraUtil;
using SiraUtil.Zenject;
using Zenject;

namespace CatCoreTesterMod.Installers
{
	internal class CatCoreInstaller : Installer<Logger, CatCoreInstance, CatCoreInstaller>
	{
		private readonly Logger _logger;
		private readonly CatCoreInstance _chatCoreInstance;

		internal CatCoreInstaller(Logger logger, CatCoreInstance chatCoreInstance)
		{
			_logger = logger;
			_chatCoreInstance = chatCoreInstance;
		}

		public override void InstallBindings()
		{
			Container.BindLoggerAsSiraLogger(_logger);
			Container.BindInstance(new UBinder<Plugin, CatCoreInstance>(_chatCoreInstance)).AsSingle();
			Container.BindInterfacesAndSelfTo<CatCoreTesterService>().AsSingle();
		}
	}
}