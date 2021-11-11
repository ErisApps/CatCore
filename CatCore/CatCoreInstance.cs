using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Exceptions;
using CatCore.Helpers;
using CatCore.Logging;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services;
using CatCore.Services.Interfaces;
using CatCore.Services.Multiplexer;
using CatCore.Services.Twitch;
using CatCore.Services.Twitch.Interfaces;
using DryIoc;
using JetBrains.Annotations;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

[assembly: InternalsVisibleTo("CatCoreTester")]
[assembly: InternalsVisibleTo("CatCoreTests")]
namespace CatCore
{
	public sealed class CatCoreInstance
	{
		private static readonly SemaphoreSlim CreationLocker = new(1, 1);
		private static readonly SemaphoreSlim RunLocker = new(1, 1);

		private static CatCoreInstance? _instance;

		private readonly MessageTemplateTextFormatter _logReceivedTextFormatter;
		private readonly Version _version;

		private Container? _container;

		private CatCoreInstance()
		{
			_logReceivedTextFormatter = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}");
			_version = typeof(CatCoreInstance).Assembly.GetName().Version;
		}

		[PublicAPI]
		public event Action<CustomLogLevel, string, string>? OnLogReceived;

		[PublicAPI]
		public static CatCoreInstance Create(Action<CustomLogLevel, string, string>? logHandler = null)
		{
			using var _ = Synchronization.Lock(CreationLocker);
			if (_instance != null)
			{
				if (logHandler != null)
				{
					_instance.OnLogReceived += logHandler;
				}

				return _instance;
			}

			_instance ??= new CatCoreInstance();
			if (logHandler != null)
			{
				_instance.OnLogReceived += logHandler;
			}

			_instance.CreateLogger();
			_instance.CreateContainer();

			return _instance;
		}

		private void CreateLogger()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.FromLogContext()
#if DEBUG
				.WriteTo.Async(writeTo => writeTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext:l}] {Message:lj}{NewLine}{Exception}",
					theme: Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme.Colored))
#endif
				.WriteTo.Conditional(_ => OnLogReceived != null, writeTo => writeTo.Async(
					writeToInternal => writeToInternal.Actionable(evt =>
					{
						using var messageWriter = new StringWriter();
						_logReceivedTextFormatter.Format(evt, messageWriter);
						OnLogReceived?.Invoke((CustomLogLevel) evt.Level, evt.Properties.TryGetValue("SourceContext", out var context) ? ((ScalarValue) context).Value.ToString() : "_",
							messageWriter.ToString());
					})))
				.CreateLogger();
		}

		private void CreateContainer()
		{
			// TODO: Remove this later on when the CatCore.Azure cold-start taking a long time has been resolved
			Scope.WaitForScopedServiceIsCreatedTimeoutTicks = 60000;

			// Create container
			_container = new Container(rules => rules
				.WithTrackingDisposableTransients()
				.WithoutThrowIfScopedOrSingletonHasTransientDependency()
				.WithoutThrowOnRegisteringDisposableTransient());

			_container.Use(_version);
			_container.Register<ConstantsBase, Constants>(Reuse.Singleton);
			_container.Register<ThreadSafeRandomFactory>(Reuse.Singleton);
			_container.Register(Made.Of(_ => ServiceInfo.Of<ThreadSafeRandomFactory>(), factory => factory.CreateNewRandom()));

			// Default logger
			_container.Register(Made.Of(() => Log.Logger), setup: Setup.With(condition: r => r.Parent.ImplementationType == null));

			// Type dependent logger
			_container.Register(
				Made.Of(() => Log.ForContext(Arg.Index<Type>(0)), r => r.Parent.ImplementationType),
				setup: Setup.With(condition: r => r.Parent.ImplementationType != null));

			// Register internal standalone services
			_container.Register<IKittenPlatformActiveStateManager, KittenPlatformActiveStateManager>(Reuse.Singleton);
			_container.Register<IKittenWebSocketProvider, KittenWebSocketProvider>(Reuse.Transient);
			_container.Register<IKittenBrowserLauncherService, KittenBrowserLauncherService>(Reuse.Singleton);
			_container.Register<IKittenPathProvider, KittenPathProvider>(Reuse.Singleton);
			_container.Register<IKittenSettingsService, KittenSettingsService>(Reuse.Singleton);
			_container.RegisterInitializer<IKittenSettingsService>((service, _) => service.Initialize());
			_container.Register<IKittenApiService, KittenApiService>(Reuse.Singleton);
			_container.RegisterInitializer<IKittenApiService>((service, _) => service.Initialize());

			// Register Twitch-specific services
			_container.Register<ITwitchAuthService, TwitchAuthService>(Reuse.Singleton);
			// .GetAwaiter().GetResult() is being used on the Initialize method to ensure the user is actually logged in when credentials are present
			_container.RegisterInitializer<ITwitchAuthService>((service, _) => service.Initialize().GetAwaiter().GetResult());
			_container.Register<ITwitchChannelManagementService, TwitchChannelManagementService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchHelixApiService, TwitchHelixApiService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchPubSubServiceManager, TwitchPubSubServiceManager>(Reuse.Singleton);
			_container.Register<ITwitchRoomStateTrackerService, TwitchRoomStateTrackerService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchUserStateTrackerService, TwitchUserStateTrackerService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchIrcService, TwitchIrcService>(Reuse.Singleton);

			_container.RegisterMany(new[] { typeof(IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>), typeof(ITwitchService) }, typeof(TwitchService), Reuse.Singleton,
				Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.RegisterMany(new[] { typeof(IKittenPlatformServiceManagerBase), typeof(TwitchServiceManager) }, typeof(TwitchServiceManager), Reuse.Singleton);

			// Register multiplexer services
			_container.Register<ChatServiceMultiplexer>(Reuse.Singleton);
			_container.Register<ChatServiceMultiplexerManager>(Reuse.Singleton);

			// Spin up internal web api service
			_ = Task.Run(() =>
			{
				var globalConfig = _container.Resolve<IKittenSettingsService>().Config.GlobalConfig;
				if (globalConfig.LaunchInternalApiOnStartup)
				{
					LaunchApiAndPortal(globalConfig.LaunchWebPortalOnStartup);
				}
			});
		}

		/// <summary>
		/// Starts all services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the generic chat multiplexer</returns>
		[PublicAPI]
		public ChatServiceMultiplexer RunAllServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			if (_container == null)
			{
				throw new CatCoreNotInitializedException();
			}

			var multiplexerManager = _container.Resolve<ChatServiceMultiplexerManager>();
			multiplexerManager.Start(Assembly.GetCallingAssembly());
			return multiplexerManager.GetMultiplexer();
		}

		/// <summary>
		/// Stops all services as soon as there aren't registered assemblies anymore.
		/// </summary>
		/// <remarks>
		/// Make sure to unregister any callbacks first!
		/// </remarks>
		[PublicAPI]
		public void StopAllServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			_container.Resolve<ChatServiceMultiplexerManager>().Stop(Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Starts the Twitch services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the Twitch service</returns>
		[PublicAPI]
		public ITwitchService RunTwitchServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			if (_container == null)
			{
				throw new CatCoreNotInitializedException();
			}

			var twitchServiceManager = _container.Resolve<TwitchServiceManager>();
			twitchServiceManager.Start(Assembly.GetCallingAssembly());
			return twitchServiceManager.GetService();
		}

		/// <summary>
		/// Stops the Twitch services as soon as there aren't registered assemblies anymore.
		/// </summary>
		/// <remarks>
		/// Make sure to unregister any callbacks first!
		/// </remarks>
		[PublicAPI]
		public void StopTwitchServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			_container.Resolve<TwitchServiceManager>().Stop(Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Initializes the internal API (if that hasn't been done yet before) and launches the web portal.
		/// </summary>
		[PublicAPI]
		public void LaunchWebPortal()
		{
			_ = Task.Run(() => LaunchApiAndPortal(true));
		}

		private void LaunchApiAndPortal(bool shouldLaunchPortal)
		{
			_ = _container.Resolve<IKittenApiService>();
			if (shouldLaunchPortal)
			{
				_container.Resolve<ILogger>().ForContext<CatCoreInstance>().Debug("Launching web portal");
				_container.Resolve<IKittenBrowserLauncherService>().LaunchWebPortal();
			}
		}

#if DEBUG
		internal Container? Container => _container;
#endif
	}
}