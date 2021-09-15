namespace CatCore.Models.Twitch.PubSub
{
	internal static class PubSubMessageTypes
	{
		public const string RESPONSE = nameof(RESPONSE);
		public const string PONG = nameof(PONG);
		public const string RECONNECT = nameof(RECONNECT);
		public const string MESSAGE = nameof(MESSAGE);
	}
}