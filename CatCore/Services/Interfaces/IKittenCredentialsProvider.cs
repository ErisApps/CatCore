using System;
using CatCore.Models.Credentials;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenCredentialsProvider<out T> : INeedInitialization where T : class, ICredentials, new()
	{
		T Credentials { get; }
		void Load();
		void Store();
		IDisposable ChangeTransaction();
	}
}