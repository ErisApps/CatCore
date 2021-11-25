namespace CatCore.Models.Twitch.IRC
{
	internal enum MessageSendingRateLimit
	{
		/// <summary>
		/// Applies to everyone who doesn't have broadcaster/moderator permissions in the channel to which the message will be send
		/// </summary>
		Normal = 20,

		/// <summary>
		/// Applies to the broadcaster and moderators of the channel to which the message will be send
		/// </summary>
		Relaxed = 100
	}
}