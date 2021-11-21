using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Config;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal sealed class KittenSettingsService : IKittenSettingsService
	{
		private const string CONFIG_FILENAME = nameof(CatCore) + "Settings.json";

		private readonly SemaphoreSlim _locker = new(1, 1);

		private readonly ILogger _logger;
		private readonly IKittenPathProvider _pathProvider;
		private readonly string _configFilePath;

		private readonly JsonSerializerOptions _jsonSerializerOptions;

		public ConfigRoot Config { get; private set; } = null!;
		public event Action<IKittenSettingsService, ConfigRoot>? OnConfigChanged;

		public KittenSettingsService(ILogger logger, IKittenPathProvider pathProvider)
		{
			_logger = logger;
			_pathProvider = pathProvider;
			_configFilePath = Path.Combine(_pathProvider.DataPath, CONFIG_FILENAME);

			_jsonSerializerOptions = new JsonSerializerOptions {WriteIndented = true};
		}

		public void Initialize()
		{
			Load();
			Store();
		}

		public void Load()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Loading {Name} settings", nameof(CatCore));

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				if (!File.Exists(_configFilePath))
				{
					Config = new ConfigRoot();
					return;
				}

				var readAllText = File.ReadAllText(_configFilePath);
				Config = JsonSerializer.Deserialize<ConfigRoot>(readAllText, _jsonSerializerOptions) ?? new ConfigRoot();
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to load the {Name} settings", nameof(CatCore));
				Config = new ConfigRoot();
			}
			finally
			{
				_locker.Release();
			}
		}

		public void Store()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Storing {Name} settings", nameof(CatCore));

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				File.WriteAllText(_configFilePath, JsonSerializer.Serialize(Config, _jsonSerializerOptions));

				OnConfigChanged?.Invoke(this, Config);
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to store the {Name} settings", nameof(CatCore));
			}
			finally
			{
				_locker.Release();
			}
		}

		public IDisposable ChangeTransaction()
		{
			return WeakActionToken.Create(this, provider => provider.Store());
		}
	}
}