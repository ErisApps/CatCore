namespace CatCore.Models.Twitch.IRC
{
	public static class IrcCommands
	{
		public const string RPL_ENDOFMOTD = "376";
		public const string PING = nameof(PING);
		public const string PONG = nameof(PONG);
		public const string JOIN = nameof(JOIN);
		public const string PART = nameof(PART);
		public const string NOTICE = nameof(NOTICE);
		public const string PRIVMSG = nameof(PRIVMSG);
	}

	public static class TwitchIrcCommands
	{
		public const string CLEARCHAT = nameof(CLEARCHAT);
		public const string CLEARMSG = nameof(CLEARMSG);
		public const string GLOBALUSERSTATE = nameof(GLOBALUSERSTATE);
		public const string ROOMSTATE = nameof(ROOMSTATE);
		public const string USERNOTICE = nameof(USERNOTICE);
		public const string USERSTATE = nameof(USERSTATE);
		public const string RECONNECT = nameof(RECONNECT);
		public const string HOSTTARGET = nameof(HOSTTARGET);
	}
}