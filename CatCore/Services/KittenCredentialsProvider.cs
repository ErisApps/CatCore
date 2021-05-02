using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Credentials;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenCredentialsProvider<T> where T : class, ICredentials, new()
	{
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

		private readonly ILogger _logger;
		private readonly IKittenPathProvider _pathProvider;

		private readonly JsonSerializerOptions _jsonSerializerOptions;

		private readonly string _credentialsFilePath;

		private string? _credentialsFileName;
		private string CredentialsFilename => _credentialsFileName ??= $"{nameof(CatCore)}Credentials - {ServiceType}.json";

		protected abstract string ServiceType { get; }

		protected T Credentials { get; private set; } = null!;

		public event Action? OnCredentialsChanged;

		protected KittenCredentialsProvider(ILogger logger, IKittenPathProvider pathProvider)
		{
			_logger = logger;
			_pathProvider = pathProvider;

			_jsonSerializerOptions = new JsonSerializerOptions {WriteIndented = true};

			_credentialsFilePath = Path.Combine(pathProvider.DataPath, CredentialsFilename);

			// Initializing internally
			Load();
			Store();
		}

		protected void Store()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Storing credentials for service {ServiceType}", ServiceType);

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				File.WriteAllText(_credentialsFilePath, JsonSerializer.Serialize(Credentials, _jsonSerializerOptions));

				OnCredentialsChanged?.Invoke();
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to store the config for service {ServiceType}", ServiceType);
			}
			finally
			{
				_locker.Release();
			}
		}

		protected IDisposable ChangeTransaction()
		{
			return WeakActionToken.Create(this, provider => provider.Store());
		}

		private void Load()
		{
			try
			{
				_locker.Wait();

				_logger.Information("Loading credentials for service {ServiceType}", ServiceType);

				if (!Directory.Exists(_pathProvider.DataPath))
				{
					Directory.CreateDirectory(_pathProvider.DataPath);
				}

				if (!File.Exists(_credentialsFilePath))
				{
					Credentials = new T();
					return;
				}

				var readAllText = File.ReadAllText(_credentialsFilePath);
				Credentials = JsonSerializer.Deserialize<T>(readAllText, _jsonSerializerOptions) ?? new T();
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to load the config for service {ServiceType}", ServiceType);
				Credentials = new T();
			}
			finally
			{
				_locker.Release();
			}
		}
	}
}