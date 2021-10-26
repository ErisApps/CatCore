using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace CatCore.Logging
{
	internal static class SinkExtensions
	{
		/// <summary>
		/// Writes log events to the EventHandler defined in <see cref="CatCoreInstance"/>.
		/// </summary>
		/// <param name="sinkConfiguration">Logger sink configuration.</param>
		/// <param name="logEventHandler">The action that will handle the event</param>
		/// <param name="restrictedToMinimumLevel">The minimum level for
		/// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
		/// <param name="levelSwitch">A switch allowing the pass-through minimum level
		/// to be changed at runtime.</param>
		/// <returns>Configuration object allowing method chaining.</returns>
		internal static LoggerConfiguration Actionable(
			this LoggerSinkConfiguration sinkConfiguration,
			Action<LogEvent> logEventHandler,
			LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
			LoggingLevelSwitch? levelSwitch = null)
		{
			if (logEventHandler == null)
			{
				throw new ArgumentNullException(nameof(logEventHandler));
			}

			return sinkConfiguration.Sink(new ActionableLogSink(logEventHandler), restrictedToMinimumLevel, levelSwitch);
		}
	}
}