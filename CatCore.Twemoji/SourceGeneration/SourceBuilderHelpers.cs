using System;
using System.Linq;
using System.Text;
using CatCore.Twemoji.Models;

namespace CatCore.Twemoji.SourceGeneration
{
	internal static class SourceBuilderHelpers
	{
		private static string Indent(uint depth = 0) => string.Empty.PadLeft((int) depth * 4).Replace("    ", "	");

		internal static void AddToSourceBuilder(this EmojiTreeRoot emojiTreeRoot, StringBuilder sourceBuilder, uint tabWidth = 0)
		{
			for (var i = 0; i < emojiTreeRoot.Count; i++)
			{
				var kvp = emojiTreeRoot.ElementAt(i);
				sourceBuilder.Append(Indent(tabWidth)).Append($"{{ '{kvp.Key}', ");

				switch (kvp.Value)
				{
					case EmojiTreeNodeBlock block:
						block.AddToSourceBuilder(sourceBuilder, tabWidth + 1);
						break;
					case EmojiTreeLeaf leaf:
						leaf.AddToSourceBuilder(sourceBuilder);
						break;
					default:
						throw new NotSupportedException();
				}

				sourceBuilder.AppendLine(i < emojiTreeRoot.Count - 1 ? "}," : "}");
			}
		}

		private static void AddToSourceBuilder(this EmojiTreeNodeBlock block, StringBuilder sourceBuilder, uint tabWidth)
		{
			sourceBuilder.AppendLine(block.Key != null
				? $"new {nameof(EmojiTreeNodeBlock)}(\"{block.Key}\", {block.Depth})"
				: $"new {nameof(EmojiTreeNodeBlock)}");
			sourceBuilder.Append(Indent(tabWidth - 1)).Append('{');

			var propertyIndentation = Indent(tabWidth);

			for (var i = 0; i < block.Count; i++)
			{
				var kvp = block.ElementAt(i);
				sourceBuilder.AppendLine().Append(propertyIndentation).Append($"{{ '{kvp.Key}', ");

				switch (kvp.Value)
				{
					case EmojiTreeNodeBlock childBlock:
						childBlock.AddToSourceBuilder(sourceBuilder, tabWidth + 1);
						sourceBuilder.Append(i < block.Count - 1 ? "}," : '}');
						break;
					case EmojiTreeLeaf childLeaf:
						childLeaf.AddToSourceBuilder(sourceBuilder);
						sourceBuilder.Append(i < block.Count - 1 ? " }," : " }");
						break;
					default:
						throw new NotSupportedException();
				}
			}

			sourceBuilder.AppendLine().Append(Indent(tabWidth - 1)).Append('}');
		}

		private static void AddToSourceBuilder(this EmojiTreeLeaf leaf, StringBuilder sourceBuilder)
		{
			sourceBuilder.Append($"new {nameof(EmojiTreeLeaf)}(\"{leaf.Key}\", {leaf.Depth})");
		}
	}
}