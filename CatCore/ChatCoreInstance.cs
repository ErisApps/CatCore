using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CatCore.Exceptions;
using CatCore.Helpers;
using CatCore.Logging;
using CatCore.Services;
using CatCore.Services.Interfaces;
using CatCore.Services.Multiplexer;
using CatCore.Services.Twitch;
using CatCore.Services.Twitch.Interfaces;
using DryIoc;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

[assembly: InternalsVisibleTo("CatCoreTester")]
namespace CatCore
{
	public class ChatCoreInstance
	{
		private static readonly SemaphoreSlim CreationLocker = new SemaphoreSlim(1, 1);
		private static readonly SemaphoreSlim RunLocker = new SemaphoreSlim(1, 1);

		private static ChatCoreInstance? _instance;

		private readonly MessageTemplateTextFormatter _logReceivedTextFormatter;
		private readonly Version _version;

		private Container? _container;

		private ChatCoreInstance()
		{
			_logReceivedTextFormatter = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}");
			_version = typeof(ChatCoreInstance).Assembly.GetName().Version;
		}

		public event Action<CustomLogLevel, string, string>? OnLogReceived;

		public static ChatCoreInstance CreateInstance(Action<CustomLogLevel, string, string>? logHandler = null)
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

			_instance ??= new ChatCoreInstance();
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
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext:l}] {Message:lj}{NewLine}{Exception}",
					theme: Serilog.Sinks.SystemConsole.Themes.SystemConsoleTheme.Colored)
#endif
				.WriteTo.Conditional(_ => OnLogReceived != null, writeTo => writeTo.Actionable(evt =>
				{
					using var messageWriter = new StringWriter();
					_logReceivedTextFormatter.Format(evt, messageWriter);
					OnLogReceived?.Invoke((CustomLogLevel) evt.Level, evt.Properties.TryGetValue("SourceContext", out var context) ? ((ScalarValue) context).Value.ToString() : "_",
						messageWriter.ToString());
				}))
				.CreateLogger();
		}

		private void CreateContainer()
		{
			// Create container
			_container = new Container(rules => rules
				.WithTrackingDisposableTransients()
				.WithoutThrowIfScopedOrSingletonHasTransientDependency()
				.WithoutThrowOnRegisteringDisposableTransient());

			_container.Use(_version);
			_container.Register<ConstantsBase, Constants>(Reuse.Singleton);
			_container.Register<ThreadSafeRandomFactory>(Reuse.Singleton);
			_container.Register<Random>(made: Made.Of(r => ServiceInfo.Of<ThreadSafeRandomFactory>(), factory => factory.CreateNewRandom()));

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
			_container.RegisterInitializer<IKittenSettingsService>((service, context) => service.Initialize());
			_container.Register<IKittenApiService, KittenApiService>(Reuse.Singleton);
			_container.RegisterInitializer<IKittenApiService>(async (service, context) => await service.Initialize());

			// Register Twitch-specific services
			_container.Register<ITwitchAuthService, TwitchAuthService>(Reuse.Singleton);
			_container.RegisterInitializer<ITwitchAuthService>((service, context) => service.Initialize().GetAwaiter().GetResult());
			_container.Register<ITwitchChannelManagementService, TwitchChannelManagementService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchHelixApiService, TwitchHelixApiService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.Register<ITwitchPubSubServiceManager, TwitchPubSubServiceManager>(Reuse.Singleton);
			_container.Register<ITwitchIrcService, TwitchIrcService>(Reuse.Singleton);

			_container.RegisterMany(new[] {typeof(IPlatformService), typeof(ITwitchService)}, typeof(TwitchService), Reuse.Singleton,
				Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));
			_container.RegisterMany(new[] {typeof(IKittenPlatformServiceManagerBase), typeof(TwitchServiceManager)}, typeof(TwitchServiceManager), Reuse.Singleton);

			// Register multiplexer services
			_container.Register<ChatServiceMultiplexer>(Reuse.Singleton);
			_container.Register<ChatServiceMultiplexerManager>(Reuse.Singleton);

			// Spin up internal web api service
			if (_container.Resolve<IKittenSettingsService>().Config.GlobalConfig.LaunchWebAppOnStartup)
			{
				LaunchWebPortal();
			}
		}

		/// <summary>
		/// Starts all services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the generic chat multiplexer</returns>
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
		public void StopAllServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			{
				_container.Resolve<ChatServiceMultiplexerManager>().Stop(Assembly.GetCallingAssembly());
			}
		}

		/// <summary>
		/// Starts the Twitch services if they haven't been already.
		/// </summary>
		/// <returns>A reference to the Twitch service</returns>
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
		public void StopTwitchServices()
		{
			using var _ = Synchronization.Lock(RunLocker);
			{
				_container.Resolve<TwitchServiceManager>().Stop(Assembly.GetCallingAssembly());
			}
		}

		public void LaunchWebPortal()
		{
			_container.Resolve<IKittenBrowserLauncherService>().LaunchWebPortal();
		}

#if DEBUG
		internal Container? Container => _container;
#endif
	}
}