using System;
using System.IO;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenPathProvider : IKittenPathProvider
	{
		public string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".chatcore");
	}
}