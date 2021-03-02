using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace CatCore.Logging
{
	public class CustomLogSink : ILogEventSink
	{
		private readonly ChatCoreInstance _chatCoreInstance;
		private readonly ITextFormatter _formatter;

		public CustomLogSink(ChatCoreInstance chatCoreInstance, ITextFormatter? formatter = null)
		{
			_chatCoreInstance = chatCoreInstance;
			_formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		}

		public void Emit(LogEvent logEvent)
		{
			using var buffer = new StringWriter();
			_formatter.Format(logEvent, buffer);
			_chatCoreInstance.OnLogReceivedInternal((CustomLogLevel) logEvent.Level, buffer.ToString());
		}
	}
}