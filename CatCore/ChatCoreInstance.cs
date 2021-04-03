﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using CatCore.Logging;
using CatCore.Services;
using CatCore.Services.Interfaces;
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
		private static readonly object _creationLock = new object();

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
			lock (_creationLock)
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

		private void CreateLogger()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.FromLogContext()
#if DEBUG
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext:l}] {Message:lj}{NewLine}{Exception}",
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
			_container.RegisterInitializer<IKittenSettingsService>((service, context) => service.Initialize());
			_container.Register<IKittenWebSocketProvider, KittenWebSocketProvider>(Reuse.Transient);
			_container.Register<IKittenApiService, KittenApiService>(Reuse.Singleton);
			_container.RegisterInitializer<IKittenApiService>(async (service, context) => await service.Initialize());

			// Register Twitch-specific services
			_container.Register<ITwitchAuthService, TwitchAuthService>(Reuse.Singleton);
			_container.RegisterInitializer<ITwitchAuthService>((service, context) => service.Initialize().GetAwaiter().GetResult());
			_container.Register<ITwitchHelixApiService, TwitchHelixApiService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));

			// TODO: Interface registration
			_container.Register<TwitchService>(Reuse.Singleton, Made.Of(FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic));

			// Spin up internal web api service
			_container.Resolve<IKittenApiService>();
		}

		public TwitchService TwitchService => _container.Resolve<TwitchService>();

#if DEBUG
		internal Container? Container => _container;
#endif
	}
}