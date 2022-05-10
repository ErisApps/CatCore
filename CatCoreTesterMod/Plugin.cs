using CatCore;
using CatCore.Logging;
using IPA;
using IPA.Logging;

namespace CatCoreTesterMod
{
	[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
	internal class Plugin
	{
		private readonly Logger _catCoreLogger;

		private readonly CatCoreInstance _catCoreInstance;

		[Init]
		public Plugin(Logger logger)
		{
			_catCoreLogger = logger.GetChildLogger(nameof(CatCore));
			_catCoreInstance = CatCoreInstance.Create(CatCoreOnLogReceived);
		}

		[OnEnable]
		public void OnEnable()
		{
			_catCoreInstance.OnLogReceived -= CatCoreOnLogReceived;
			_catCoreInstance.OnLogReceived += CatCoreOnLogReceived;
		}

		[OnDisable]
		public void OnDisable()
		{
			_catCoreInstance.OnLogReceived -= CatCoreOnLogReceived;
		}

		private void CatCoreOnLogReceived(CustomLogLevel level, string context, string message)
		{
			_catCoreLogger.Log(level switch
			{
				CustomLogLevel.Trace => Logger.Level.Trace,
				CustomLogLevel.Debug => Logger.Level.Debug,
				CustomLogLevel.Information => Logger.Level.Info,
				CustomLogLevel.Warning => Logger.Level.Warning,
				CustomLogLevel.Error => Logger.Level.Error,
				CustomLogLevel.Critical => Logger.Level.Critical,
				_ => Logger.Level.Debug
			}, $"{context} | {message}");
		}
	}
}