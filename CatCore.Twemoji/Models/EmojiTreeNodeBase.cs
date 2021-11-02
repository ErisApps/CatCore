using System.Collections.Generic;

namespace CatCore.Twemoji.Models
{
	public abstract class EmojiTreeNodeBase : Dictionary<char, IEmojiTreeLeaf>
	{
		internal void AddToTree(string formattedKey, char[] codepoints, uint depth = 0)
		{
			var key = codepoints[depth];
			if (TryGetValue(key, out var node))
			{
				if (codepoints.Length - 1 == depth)
				{
					if (node is EmojiTreeNodeBlock block)
					{
						block.Key = formattedKey;
						block.Depth = depth;
					}
				}
				else
				{
					EmojiTreeNodeBlock block;
					if (node is EmojiTreeLeaf leaf)
					{
						block = leaf.UpgradeToBlock();
						this[key] = block;
					}
					else
					{
						block = (EmojiTreeNodeBlock) node;
					}

					block.AddToTree(formattedKey, codepoints, ++depth);
				}
			}
			else
			{
				IEmojiTreeLeaf newNode;
				if (codepoints.Length - 1 == depth)
				{
					newNode = new EmojiTreeLeaf(formattedKey, depth);
				}
				else
				{
					newNode = new EmojiTreeNodeBlock();
					((EmojiTreeNodeBlock) newNode).AddToTree(formattedKey, codepoints, ++depth);
				}

				Add(key, newNode);
			}
		}

		internal IEmojiTreeLeaf? LookupLeaf(string text, int startPos)
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