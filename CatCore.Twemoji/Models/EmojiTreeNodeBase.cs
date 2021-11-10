using System.Collections.Generic;

namespace CatCore.Twemoji.Models
{
	public abstract class EmojiTreeNodeBase : Dictionary<char, IEmojiTreeLeaf>
	{
		public IEmojiTreeLeaf? LookupLeaf(string text, int startPos)
		{
			if (text.Length <= startPos)
			{
				return null;
			}

			if (TryGetValue(text[startPos], out var node))
			{
				if (node is EmojiTreeNodeBlock block)
				{
					return block.LookupLeaf(text, ++startPos);
				}

				if (node is EmojiTreeLeaf leaf)
				{
					return leaf;
				}
			}
			else if (this is EmojiTreeNodeBlock { Key: { } } @this)
			{
				return @this;
			}

			return null;
		}
	}
}