using System;
using System.Runtime.CompilerServices;
using CatCore.Logging;
using CatCore.Services;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch;
using CatCore.Services.Twitch.Interfaces;
using DryIoc;
using Serilog;

[assembly: InternalsVisibleTo("CatCoreTester")]
namespace CatCore
{
	public class ChatCoreInstance
	{
		private static readonly object CreationLock = new object();

		private static ChatCoreInstance? _instance;

		private Container? _container;

		private ChatCoreInstance()
		{
			Version = typeof(ChatCoreInstance).Assembly.GetName().Version;
		}

		internal Version Version { get; }

		public event Action<CustomLogLevel, string>? OnLogReceived;

		public static ChatCoreInstance CreateInstance(Action<CustomLogLevel, string>? logHandler = null)
		{
			lock (CreationLock)
			{
				if (_instance != null)
				{
					if (logHandler != null)
					{
						_instance.OnLogReceived += logHandler;
					}

					return _instance;
				}

				_instance ??= new ChatCoreInstance();
				if (logHandler != null)
				{
					_instance.OnLogReceived += logHandler;
				}

				_instance.CreateLogger();
				_instance.CreateContainer();

				return _instance;
			}
		}

		internal void OnLogReceivedInternal(CustomLogLevel logLevel, string message)
		{
			OnLogReceived?.Invoke(logLevel, message);
		}

		private void CreateLogger()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.FromLogContext()
#if DEBUG
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext:l}] {Message:lj}{NewLine}{Exception}",
					theme: Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme.Colored)
#endif
				.WriteTo.Conditional(_ => OnLogReceived != null, writeTo => writeTo.CustomLogSink(this))
				.CreateLogger();
		}

		private void CreateContainer()
		{
			// Create container
			_container = new Container(rules => rules
				.WithTrackingDisposableTransients()
				.WithoutThrowIfScopedOrSingletonHasTransientDependency()
				.WithoutThrowOnRegisteringDisposableTransient());

			_container.Use(Version);
			_container.Register<ConstantsBase, Constants>(Reuse.Singleton);

			// Default logger
			_container.Register(Made.Of(() => Log.Logger), setup: Setup.With(condition: r => r.Parent.ImplementationType == null));
			// Type dependent logger
			_container.Register(
				Made.Of(() => Log.ForContext(Arg.Index<Type>(0)), r => r.Parent.ImplementationType),
				setup: Setup.With(condition: r => r.Parent.ImplementationType != null));

			// Register internal standalone services
			_container.Register<IKittenBrowserLauncherService, KittenBrowserLauncherService>(Reuse.Singleton);
			_container.Register<IKittenPathProvider, KittenPathProvider>(Reuse.Singleton);
			_container.Register<IKittenSettingsService, KittenSettingsService>(Reuse.Singleton);

			_container.Register<IKittenWebSocketProvider, KittenWebSocketProvider>(Reuse.Transient);

			_container.Register<IKittenApiService, KittenApiService>(Reuse.Singleton);
			_container.RegisterInitializer<IKittenApiService>((service, context) => service.Initialize());

			// Register Twitch-specific services
			_container.Register<ITwitchCredentialsProvider, TwitchCredentialsProvider>();
			_container.RegisterInitializer<ITwitchCredentialsProvider>((service, context) => service.Initialize());

			_container.Register<ITwitchAuthService, TwitchAuthService>(Reuse.Singleton);
			_container.RegisterInitializer<ITwitchAuthService>((service, context) => service.Initialize());

			_container.Register<ITwitchHelixApiService, TwitchHelixApiService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			// Spin up internal web api service
			_container.Resolve<IKittenApiService>();
		}

#if DEBUG
		internal Container? Container => _container;
#endif
	}
}