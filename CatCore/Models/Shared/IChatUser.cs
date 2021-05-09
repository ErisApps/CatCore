namespace CatCore.Models.Shared
{
	public interface IChatUser
	{
		string Id { get; }
		string UserName { get; }
		string DisplayName { get; }
		string Color { get; }
		bool IsBroadcaster { get; }
		bool IsModerator { get; }
	}
}