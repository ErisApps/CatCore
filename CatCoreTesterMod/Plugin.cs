using CatCore;
using CatCore.Logging;
using IPA;
using IPA.Logging;

namespace CatCoreTesterMod
{
	[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
	internal class Plugin
	{
		[Init]
		public Plugin(Logger logger)
		{
			CatCoreInstance.Create((level, context, message) => logger
				.GetChildLogger("CatCore")
				.Log(level switch
				{
					CustomLogLevel.Trace => Logger.Level.Trace,
					CustomLogLevel.Debug => Logger.Level.Debug,
					CustomLogLevel.Information => Logger.Level.Info,
					CustomLogLevel.Warning => Logger.Level.Warning,
					CustomLogLevel.Error => Logger.Level.Error,
					CustomLogLevel.Critical => Logger.Level.Critical,
					_ => Logger.Level.Debug
				}, $"{context} | {message}"));
		}
	}
}