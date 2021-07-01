using System;
using System.Threading;
using CatCore.Models.Config;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenRawSocketProvider : INeedInitialization, IDisposable
	{
		bool isServerRunning();
	}
}