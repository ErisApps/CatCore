using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenSettingsService : IKittenSettingsService
	{
		private readonly IKittenPathProvider _pathProvider;

		public KittenSettingsService(IKittenPathProvider pathProvider)
		{
			_pathProvider = pathProvider;
		}
	}
}