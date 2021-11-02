namespace CatCore.Twemoji.Models
{
	public interface IEmojiTreeLeaf
	{
		string? Key { get; }
		uint Depth { get; }
		string Url { get; }
	}
}