using System;
using System.IO;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal sealed class KittenPathProvider : IKittenPathProvider
	{
		private string? _dataPath;

		public string DataPath => _dataPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $".{nameof(CatCore).ToLower()}");
	}
}