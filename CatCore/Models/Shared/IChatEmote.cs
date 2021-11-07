namespace CatCore.Models.Shared
{
	public interface IChatEmote
	{
		string Id { get; }
		string Name { get; }
		int StartIndex { get; }
		int EndIndex { get; }
		string Url { get; }
		bool Animated { get; }
	}
}