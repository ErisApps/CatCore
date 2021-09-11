using CatCore;
using CatCore.Logging;
using CatCoreTesterMod.Installers;
using IPA;
using IPA.Logging;
using SiraUtil.Zenject;

namespace CatCoreTesterMod
{
	[Plugin(RuntimeOptions.DynamicInit)]
	internal class Plugin
	{
		[Init]
		public Plugin(Logger logger, Zenjector zenjector)
		{
			zenjector.OnApp<CatCoreInstaller>().WithParameters(logger, ChatCoreInstance.CreateInstance((level, context, message) => logger
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
				}, $"{context} | {message}")));
		}

		[OnEnable, OnDisable]
		public void OnState()
		{
			/* Plugin State Changed */
		}
	}
}