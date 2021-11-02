namespace CatCore.Twemoji.Models
{
	public class EmojiTreeNodeBlock : EmojiTreeNodeBase, IEmojiTreeLeaf
	{
#pragma warning disable CS8766
		public string? Key { get; internal set; }
#pragma warning restore CS8766
		public int Depth { get; internal set; }

		public string Url => Key != null ? $"https://twemoji.maxcdn.com/v/latest/72x72/{Key}.png" : string.Empty;

		public EmojiTreeNodeBlock()
		{
		}

		public EmojiTreeNodeBlock(string key, int depth)
		{
			Key = key;
			Depth = depth;
		}
	}
}