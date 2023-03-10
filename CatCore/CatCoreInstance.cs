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
using CatCore.Services.Twitch.Media;
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

		private IResolver? _resolver;

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

			_instance.SpinUpApiAndPortalOnStartupIfRequired();

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
						OnLogReceived?.Invoke(
							(CustomLogLevel) evt.Level,
							evt.Properties.TryGetValue("SourceContext", out var contextRaw) && contextRaw is ScalarValue { Value: {} } context
								? context.Value.ToString()
								: "_",
							messageWriter.ToString());
					})))
				.CreateLogger();
		}

		private void CreateContainer()
		{
			// TODO: Remove this later on when the CatCore.Azure cold-start taking a long time has been resolved
			Scope.WaitForScopedServiceIsCreatedTimeoutTicks = 60000;

			// Create container
			var container = new Container(rules => rules
				.WithTrackingDisposableTransients()
				.WithoutThrowIfScopedOrSingletonHasTransientDependency()
				.WithoutThrowOnRegisteringDisposableTransient());

			container.Use(_version);
			container.Register<ConstantsBase, Constants>(Reuse.Singleton);
			container.Register<ThreadSafeRandomFactory>(Reuse.Singleton);
			container.Register(Made.Of(_ => ServiceInfo.Of<ThreadSafeRandomFactory>(), factory => factory.CreateNewRandom()));

			// Default logger
			container.Register(Made.Of(() => Log.Logger), setup: Setup.With(condition: r => r.Parent.ImplementationType == null));

			// Type dependent logger
			container.Register(
				Made.Of(() => Log.ForContext(Arg.Index<Type>(0)), r => r.Parent.ImplementationType),
				setup: Setup.With(condition: r => r.Parent.ImplementationType != null));

			// Register internal standalone services
			container.Register<IKittenPlatformActiveStateManager, KittenPlatformActiveStateManager>(Reuse.Singleton);
			container.Register<IKittenWebSocketProvider, KittenWebSocketProvider>(Reuse.Transient);
			container.Register<IKittenBrowserLauncherService, KittenBrowserLauncherService>(Reuse.Singleton);
			container.Register<IKittenPathProvider, KittenPathProvider>(Reuse.Singleton);
			container.Register<IKittenSettingsService, KittenSettingsService>(Reuse.Singleton);
			container.RegisterInitializer<IKittenSettingsService>((service, _) => service.Initialize());
			container.Register<IKittenApiService, KittenApiService>(Reuse.Singleton);
			container.RegisterInitializer<IKittenApiService>((service, _) => service.Initialize());

			container.Register<BttvDataProvider>(Reuse.Singleton);

			// Register Twitch-specific services
			container.Register<ITwitchAuthService, TwitchAuthService>(Reuse.Singleton);
			container.Register<ITwitchChannelManagementService, TwitchChannelManagementService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			container.Register<ITwitchHelixApiService, TwitchHelixApiService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			container.Register<ITwitchPubSubServiceManager, TwitchPubSubServiceManager>(Reuse.Singleton);
			container.Register<ITwitchRoomStateTrackerService, TwitchRoomStateTrackerService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			container.Register<ITwitchUserStateTrackerService, TwitchUserStateTrackerService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			container.Register<TwitchBadgeDataProvider>(Reuse.Singleton);
			container.Register<TwitchCheermoteDataProvider>(Reuse.Singleton);
			container.Register<TwitchMediaDataProvider>(Reuse.Singleton);
			container.Register<TwitchEmoteDetectionHelper>(Reuse.Singleton);
			container.Register<ITwitchIrcService, TwitchIrcService>(Reuse.Singleton);

			container.RegisterMany(new[] { typeof(IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>), typeof(ITwitchService) }, typeof(TwitchService), Reuse.Singleton,
				Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			container.RegisterMany(new[] { typeof(IKittenPlatformServiceManagerBase), typeof(TwitchServiceManager) }, typeof(TwitchServiceManager), Reuse.Singleton);

			// Register multiplexer services
			container.Register( // manually register Twitch service to multiplexer, TODO: make a general form of this for all IPlatformServices
				made: Made.Of(() => MultiplexedPlatformService.From<ITwitchService, TwitchChannel, TwitchMessage>(Arg.Of<ITwitchService>())));
			container.Register<ChatServiceMultiplexer>(Reuse.Singleton);
			container.Register<ChatServiceMultiplexerManager>(Reuse.Singleton);

			_resolver = container.WithNoMoreRegistrationAllowed();
		}

		// TODO: handle all of the start/stop tasks

		/// <summary>
		/// Starts all services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the generic chat multiplexer</returns>
		[PublicAPI]
		public ChatServiceMultiplexer RunAllServices()
		{
			using var __ = Synchronization.Lock(RunLocker);
			if (_resolver == null)
			{
				throw new CatCoreNotInitializedException();
			}

			var multiplexerManager = _resolver.Resolve<ChatServiceMultiplexerManager>();
			_ = multiplexerManager.Start(Assembly.GetCallingAssembly());
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
			using var __ = Synchronization.Lock(RunLocker);
			_ = _resolver.Resolve<ChatServiceMultiplexerManager>().Stop(Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Starts the Twitch services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the Twitch service</returns>
		[PublicAPI]
		public ITwitchService RunTwitchServices()
		{
			using var __ = Synchronization.Lock(RunLocker);
			if (_resolver == null)
			{
				throw new CatCoreNotInitializedException();
			}

			var twitchServiceManager = _resolver.Resolve<TwitchServiceManager>();
			_ = twitchServiceManager.Start(Assembly.GetCallingAssembly());
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
			using var __ = Synchronization.Lock(RunLocker);
			_ = _resolver.Resolve<TwitchServiceManager>().Stop(Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Initializes the internal API (if that hasn't been done yet before) and launches the web portal.
		/// </summary>
		[PublicAPI]
		public void LaunchWebPortal()
		{
			_ = Task.Run(() => LaunchApiAndPortal(true));
		}

		private void SpinUpApiAndPortalOnStartupIfRequired()
		{
			// Spin up internal web api service
			Task.Run(() =>
			{
				var globalConfig = _resolver.Resolve<IKittenSettingsService>().Config.GlobalConfig;
				if (globalConfig.LaunchInternalApiOnStartup)
				{
					LaunchApiAndPortal(globalConfig.LaunchWebPortalOnStartup);
				}
			});
		}

		private void LaunchApiAndPortal(bool shouldLaunchPortal)
		{
			_ = _resolver.Resolve<IKittenApiService>();
			if (shouldLaunchPortal)
			{
				_resolver.Resolve<ILogger>().ForContext<CatCoreInstance>().Debug("Launching web portal");
				_resolver.Resolve<IKittenBrowserLauncherService>().LaunchWebPortal();
			}
		}

#if DEBUG
		internal IResolver? Resolver => _resolver;
#endif
	}
}