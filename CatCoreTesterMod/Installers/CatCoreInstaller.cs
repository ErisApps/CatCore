using CatCore;
using CatCoreTesterMod.Services;
using SiraUtil.Zenject;
using Zenject;

namespace CatCoreTesterMod.Installers
{
	internal class CatCoreInstaller : Installer
	{
		private readonly CatCoreInstance _chatCoreInstance;

		internal CatCoreInstaller(CatCoreInstance chatCoreInstance) => _chatCoreInstance = chatCoreInstance;

		public override void InstallBindings()
		{
			Container.BindInstance(new UBinder<Plugin, CatCoreInstance>(_chatCoreInstance)).AsSingle();
			Container.BindInterfacesAndSelfTo<CatCoreTesterService>().AsSingle();
		}
	}
}