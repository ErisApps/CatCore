using System;
using CatCore.Models.Config;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenSettingsService : INeedInitialization
	{
		ConfigRoot Config { get; }
		void Load();
		void Store();
		IDisposable ChangeTransaction();
	}
}