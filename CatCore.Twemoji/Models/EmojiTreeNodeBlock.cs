namespace CatCore.Twemoji.Models
{
	public class EmojiTreeNodeBlock : EmojiTreeNodeBase, IEmojiTreeLeaf
	{
		public string? Key { get; internal set; }
		public uint Depth { get; internal set; }

		public string Url => Key != null ? $"https://twemoji.maxcdn.com/v/latest/72x72/{Key}.png" : string.Empty;

		public EmojiTreeNodeBlock()
		{
		}

		public EmojiTreeNodeBlock(string key, uint depth)
		{
			Key = key;
			Depth = depth;
		}
	}
}