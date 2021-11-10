using CatCore.Twemoji.Models;

namespace CatCore.Twemoji.SourceGeneration.Extensions
{
	internal static class EmojiTreeNodeBaseExtensions
	{
		// ReSharper disable once CognitiveComplexity
		internal static void AddToTree(this EmojiTreeNodeBase emojiTreeNodeBase, string formattedKey, char[] codepoints, int depth = 0)
		{
			var key = codepoints[depth];
			if (emojiTreeNodeBase.TryGetValue(key, out var node))
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
						emojiTreeNodeBase[key] = block;
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

				emojiTreeNodeBase.Add(key, newNode);
			}
		}
	}
}