namespace CatCore.Twemoji.Models
{
	public class EmojiTreeLeaf : IEmojiTreeLeaf
	{
		public string Key { get; }
		public int Depth { get; }

		public string Url => $"https://twemoji.maxcdn.com/v/latest/72x72/{Key}.png";

		public EmojiTreeLeaf(string key, int depth)
		{
			Key = key;
			Depth = depth;
		}

		public EmojiTreeNodeBlock UpgradeToBlock()
		{
			return new EmojiTreeNodeBlock { Key = Key, Depth = Depth };
		}
	}
}