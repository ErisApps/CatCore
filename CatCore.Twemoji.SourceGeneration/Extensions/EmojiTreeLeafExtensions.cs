using CatCore.Twemoji.Models;

namespace CatCore.Twemoji.SourceGeneration.Extensions
{
	internal static class EmojiTreeLeafExtensions
	{
		internal static EmojiTreeNodeBlock UpgradeToBlock(this EmojiTreeLeaf emojiTreeLeaf)
		{
			return new EmojiTreeNodeBlock(emojiTreeLeaf.Key, emojiTreeLeaf.Depth);
		}
	}
}