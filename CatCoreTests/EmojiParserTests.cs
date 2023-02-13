using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatCore.Emoji.Models;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class EmojiParserTests
	{
		public enum Status
		{
			Component,
			FullyQualified,
			MinimallyQualified,
			Unqualified
		}

		public static IEnumerable<object[]> EmojiTestData =>
			File.ReadLines(Path.Combine(Environment.CurrentDirectory, "Resources", "Unicode14_0EmojiTest.txt"))
				.Where(line => !string.IsNullOrWhiteSpace(line) && line[0] != '#')
				.Select(line =>
				{
					var splitEntries = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

					var splitEntriesIndexCursor = splitEntries.IndexOf(";");
					var codepointsRepresentation = string.Join(" ", splitEntries.Take(splitEntriesIndexCursor));

					var status = Enum.Parse<Status>(splitEntries[++splitEntriesIndexCursor].Replace("-", string.Empty), true);

					var emojiRepresentation = splitEntries[splitEntriesIndexCursor += 2];
					var unicodeVersionIntroduced = splitEntries[++splitEntriesIndexCursor];

					var emoteDescription = string.Join(" ", splitEntries.Skip(splitEntriesIndexCursor));

					return new object[] { line, codepointsRepresentation, status, emojiRepresentation, unicodeVersionIntroduced, emoteDescription };
				})
				.Where(x => ((Status)x[2]) == Status.FullyQualified);

		[Theory]
		[MemberData(nameof(EmojiTestData))]
#pragma warning disable xUnit1026
		public void FrwTwemojiBaselineTest(string line, string codepointsRepresentation, Status status, string emojiRepresentation, string unicodeVersionIntroduced, string emoteDescription)
#pragma warning restore xUnit1026
		{
			// Arrange
			// NOP

			// Act
			var matchedEmojis = TwemojiBaselineImplementation.FindEmojis(line).ToList();

			// Assert
			matchedEmojis.Should().HaveCount(1);
			matchedEmojis.Should().HaveElementAt(0, codepointsRepresentation.Replace(" ", "-"), $"{emoteDescription} introduced in {unicodeVersionIntroduced}");

			// Not asserting failure conditions because it might either result in no matches or in a match of a fully-qualified subset emote
		}

		[Theory]
		[MemberData(nameof(EmojiTestData))]
#pragma warning disable xUnit1026
		public void CatCoreTwemojiTest(string line, string codepointsRepresentation, Status status, string emojiRepresentation, string unicodeVersionIntroduced, string emoteDescription)
#pragma warning restore xUnit1026
		{
			// Arrange
			// NOP

			// Act
			IEmojiTreeLeaf? foundEmojiLeaf = null;
			for (var i = 0; i < line.Length; i++)
			{
				foundEmojiLeaf = CatCore.Emoji.Twemoji.EmojiTesting.EmojiReferenceData.LookupLeaf(line, i);
				if (foundEmojiLeaf != null)
				{
					break;
				}
			}

			// Assert
			foundEmojiLeaf.Should().NotBeNull();
			foundEmojiLeaf!.Key.Should().Be(codepointsRepresentation.Replace(" ", "-").ToLowerInvariant());
			foundEmojiLeaf.Depth.Should().Be(emojiRepresentation.ToCharArray().Length - 1);

			// Not asserting failure conditions because it might either result in no matches or in a match of a fully-qualified subset emote
		}

		#region FrwTwemoji Baseline implementation

		public static class TwemojiBaselineImplementation
		{
			private static readonly Regex EmojiRegex =
				new Regex(
					"(?:\\uD83D\\uDC68\\uD83C\\uDFFC\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C\\uDFFB|\\uD83D\\uDC68\\uD83C\\uDFFD\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB\\uDFFC]|\\uD83D\\uDC68\\uD83C\\uDFFE\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB-\\uDFFD]|\\uD83D\\uDC68\\uD83C\\uDFFF\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB-\\uDFFE]|\\uD83D\\uDC69\\uD83C\\uDFFB\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFC-\\uDFFF]|\\uD83D\\uDC69\\uD83C\\uDFFC\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB\\uDFFD-\\uDFFF]|\\uD83D\\uDC69\\uD83C\\uDFFC\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC69\\uD83C\\uDFFB|\\uD83D\\uDC69\\uD83C\\uDFFD\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB\\uDFFC\\uDFFE\\uDFFF]|\\uD83D\\uDC69\\uD83C\\uDFFD\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC69\\uD83C[\\uDFFB\\uDFFC]|\\uD83D\\uDC69\\uD83C\\uDFFE\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB-\\uDFFD\\uDFFF]|\\uD83D\\uDC69\\uD83C\\uDFFE\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC69\\uD83C[\\uDFFB-\\uDFFD]|\\uD83D\\uDC69\\uD83C\\uDFFF\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC68\\uD83C[\\uDFFB-\\uDFFE]|\\uD83D\\uDC69\\uD83C\\uDFFF\\u200D\\uD83E\\uDD1D\\u200D\\uD83D\\uDC69\\uD83C[\\uDFFB-\\uDFFE]|\\uD83E\\uDDD1\\uD83C\\uDFFB\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1\\uD83C\\uDFFB|\\uD83E\\uDDD1\\uD83C\\uDFFC\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1\\uD83C[\\uDFFB\\uDFFC]|\\uD83E\\uDDD1\\uD83C\\uDFFD\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1\\uD83C[\\uDFFB-\\uDFFD]|\\uD83E\\uDDD1\\uD83C\\uDFFE\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1\\uD83C[\\uDFFB-\\uDFFE]|\\uD83E\\uDDD1\\uD83C\\uDFFF\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1\\uD83C[\\uDFFB-\\uDFFF]|\\uD83E\\uDDD1\\u200D\\uD83E\\uDD1D\\u200D\\uD83E\\uDDD1|\\uD83D\\uDC6B\\uD83C[\\uDFFB-\\uDFFF]|\\uD83D\\uDC6C\\uD83C[\\uDFFB-\\uDFFF]|\\uD83D\\uDC6D\\uD83C[\\uDFFB-\\uDFFF]|\\uD83D[\\uDC6B-\\uDC6D])|(?:\\uD83D[\\uDC68\\uDC69])(?:\\uD83C[\\uDFFB-\\uDFFF])?\\u200D(?:\\u2695\\uFE0F|\\u2696\\uFE0F|\\u2708\\uFE0F|\\uD83C[\\uDF3E\\uDF73\\uDF93\\uDFA4\\uDFA8\\uDFEB\\uDFED]|\\uD83D[\\uDCBB\\uDCBC\\uDD27\\uDD2C\\uDE80\\uDE92]|\\uD83E[\\uDDAF-\\uDDB3\\uDDBC\\uDDBD])|(?:\\uD83C[\\uDFCB\\uDFCC]|\\uD83D[\\uDD74\\uDD75]|\\u26F9)((?:\\uD83C[\\uDFFB-\\uDFFF]|\\uFE0F)\\u200D[\\u2640\\u2642]\\uFE0F)|(?:\\uD83C[\\uDFC3\\uDFC4\\uDFCA]|\\uD83D[\\uDC6E\\uDC71\\uDC73\\uDC77\\uDC81\\uDC82\\uDC86\\uDC87\\uDE45-\\uDE47\\uDE4B\\uDE4D\\uDE4E\\uDEA3\\uDEB4-\\uDEB6]|\\uD83E[\\uDD26\\uDD35\\uDD37-\\uDD39\\uDD3D\\uDD3E\\uDDB8\\uDDB9\\uDDCD-\\uDDCF\\uDDD6-\\uDDDD])(?:\\uD83C[\\uDFFB-\\uDFFF])?\\u200D[\\u2640\\u2642]\\uFE0F|(?:\\uD83D\\uDC68\\u200D\\u2764\\uFE0F\\u200D\\uD83D\\uDC8B\\u200D\\uD83D\\uDC68|\\uD83D\\uDC68\\u200D\\uD83D\\uDC68\\u200D\\uD83D\\uDC66\\u200D\\uD83D\\uDC66|\\uD83D\\uDC68\\u200D\\uD83D\\uDC68\\u200D\\uD83D\\uDC67\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC68\\u200D\\uD83D\\uDC69\\u200D\\uD83D\\uDC66\\u200D\\uD83D\\uDC66|\\uD83D\\uDC68\\u200D\\uD83D\\uDC69\\u200D\\uD83D\\uDC67\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC69\\u200D\\u2764\\uFE0F\\u200D\\uD83D\\uDC8B\\u200D\\uD83D[\\uDC68\\uDC69]|\\uD83D\\uDC69\\u200D\\uD83D\\uDC69\\u200D\\uD83D\\uDC66\\u200D\\uD83D\\uDC66|\\uD83D\\uDC69\\u200D\\uD83D\\uDC69\\u200D\\uD83D\\uDC67\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC68\\u200D\\u2764\\uFE0F\\u200D\\uD83D\\uDC68|\\uD83D\\uDC68\\u200D\\uD83D\\uDC66\\u200D\\uD83D\\uDC66|\\uD83D\\uDC68\\u200D\\uD83D\\uDC67\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC68\\u200D\\uD83D\\uDC68\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC68\\u200D\\uD83D\\uDC69\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC69\\u200D\\u2764\\uFE0F\\u200D\\uD83D[\\uDC68\\uDC69]|\\uD83D\\uDC69\\u200D\\uD83D\\uDC66\\u200D\\uD83D\\uDC66|\\uD83D\\uDC69\\u200D\\uD83D\\uDC67\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC69\\u200D\\uD83D\\uDC69\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83C\\uDFF3\\uFE0F\\u200D\\u26A7\\uFE0F|\\uD83C\\uDFF3\\uFE0F\\u200D\\uD83C\\uDF08|\\uD83C\\uDFF4\\u200D\\u2620\\uFE0F|\\uD83D\\uDC15\\u200D\\uD83E\\uDDBA|\\uD83D\\uDC41\\u200D\\uD83D\\uDDE8|\\uD83D\\uDC68\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC69\\u200D\\uD83D[\\uDC66\\uDC67]|\\uD83D\\uDC6F\\u200D\\u2640\\uFE0F|\\uD83D\\uDC6F\\u200D\\u2642\\uFE0F|\\uD83E\\uDD3C\\u200D\\u2640\\uFE0F|\\uD83E\\uDD3C\\u200D\\u2642\\uFE0F|\\uD83E\\uDDDE\\u200D\\u2640\\uFE0F|\\uD83E\\uDDDE\\u200D\\u2642\\uFE0F|\\uD83E\\uDDDF\\u200D\\u2640\\uFE0F|\\uD83E\\uDDDF\\u200D\\u2642\\uFE0F)|[#*0-9]\\uFE0F?\\u20E3|(?:[©®\\u2122\\u265F]\\uFE0F)|(?:\\uD83C[\\uDC04\\uDD70\\uDD71\\uDD7E\\uDD7F\\uDE02\\uDE1A\\uDE2F\\uDE37\\uDF21\\uDF24-\\uDF2C\\uDF36\\uDF7D\\uDF96\\uDF97\\uDF99-\\uDF9B\\uDF9E\\uDF9F\\uDFCD\\uDFCE\\uDFD4-\\uDFDF\\uDFF3\\uDFF5\\uDFF7]|\\uD83D[\\uDC3F\\uDC41\\uDCFD\\uDD49\\uDD4A\\uDD6F\\uDD70\\uDD73\\uDD76-\\uDD79\\uDD87\\uDD8A-\\uDD8D\\uDDA5\\uDDA8\\uDDB1\\uDDB2\\uDDBC\\uDDC2-\\uDDC4\\uDDD1-\\uDDD3\\uDDDC-\\uDDDE\\uDDE1\\uDDE3\\uDDE8\\uDDEF\\uDDF3\\uDDFA\\uDECB\\uDECD-\\uDECF\\uDEE0-\\uDEE5\\uDEE9\\uDEF0\\uDEF3]|[\\u203C\\u2049\\u2139\\u2194-\\u2199\\u21A9\\u21AA\\u231A\\u231B\\u2328\\u23CF\\u23ED-\\u23EF\\u23F1\\u23F2\\u23F8-\\u23FA\\u24C2\\u25AA\\u25AB\\u25B6\\u25C0\\u25FB-\\u25FE\\u2600-\\u2604\\u260E\\u2611\\u2614\\u2615\\u2618\\u2620\\u2622\\u2623\\u2626\\u262A\\u262E\\u262F\\u2638-\\u263A\\u2640\\u2642\\u2648-\\u2653\\u2660\\u2663\\u2665\\u2666\\u2668\\u267B\\u267F\\u2692-\\u2697\\u2699\\u269B\\u269C\\u26A0\\u26A1\\u26A7\\u26AA\\u26AB\\u26B0\\u26B1\\u26BD\\u26BE\\u26C4\\u26C5\\u26C8\\u26CF\\u26D1\\u26D3\\u26D4\\u26E9\\u26EA\\u26F0-\\u26F5\\u26F8\\u26FA\\u26FD\\u2702\\u2708\\u2709\\u270F\\u2712\\u2714\\u2716\\u271D\\u2721\\u2733\\u2734\\u2744\\u2747\\u2757\\u2763\\u2764\\u27A1\\u2934\\u2935\\u2B05-\\u2B07\\u2B1B\\u2B1C\\u2B50\\u2B55\\u3030\\u303D\\u3297\\u3299])(?:\\uFE0F|(?!\\uFE0E))|(?:(?:\\uD83C[\\uDFCB\\uDFCC]|\\uD83D[\\uDD74\\uDD75\\uDD90]|[\\u261D\\u26F7\\u26F9\\u270C\\u270D])(?:\\uFE0F|(?!\\uFE0E))|(?:\\uD83C[\\uDF85\\uDFC2-\\uDFC4\\uDFC7\\uDFCA]|\\uD83D[\\uDC42\\uDC43\\uDC46-\\uDC50\\uDC66-\\uDC69\\uDC6E\\uDC70-\\uDC78\\uDC7C\\uDC81-\\uDC83\\uDC85-\\uDC87\\uDCAA\\uDD7A\\uDD95\\uDD96\\uDE45-\\uDE47\\uDE4B-\\uDE4F\\uDEA3\\uDEB4-\\uDEB6\\uDEC0\\uDECC]|\\uD83E[\\uDD0F\\uDD18-\\uDD1C\\uDD1E\\uDD1F\\uDD26\\uDD30-\\uDD39\\uDD3D\\uDD3E\\uDDB5\\uDDB6\\uDDB8\\uDDB9\\uDDBB\\uDDCD-\\uDDCF\\uDDD1-\\uDDDD]|[\\u270A\\u270B]))(?:\\uD83C[\\uDFFB-\\uDFFF])?|(?:\\uD83C\\uDFF4\\uDB40\\uDC67\\uDB40\\uDC62\\uDB40\\uDC65\\uDB40\\uDC6E\\uDB40\\uDC67\\uDB40\\uDC7F|\\uD83C\\uDFF4\\uDB40\\uDC67\\uDB40\\uDC62\\uDB40\\uDC73\\uDB40\\uDC63\\uDB40\\uDC74\\uDB40\\uDC7F|\\uD83C\\uDFF4\\uDB40\\uDC67\\uDB40\\uDC62\\uDB40\\uDC77\\uDB40\\uDC6C\\uDB40\\uDC73\\uDB40\\uDC7F|\\uD83C\\uDDE6\\uD83C[\\uDDE8-\\uDDEC\\uDDEE\\uDDF1\\uDDF2\\uDDF4\\uDDF6-\\uDDFA\\uDDFC\\uDDFD\\uDDFF]|\\uD83C\\uDDE7\\uD83C[\\uDDE6\\uDDE7\\uDDE9-\\uDDEF\\uDDF1-\\uDDF4\\uDDF6-\\uDDF9\\uDDFB\\uDDFC\\uDDFE\\uDDFF]|\\uD83C\\uDDE8\\uD83C[\\uDDE6\\uDDE8\\uDDE9\\uDDEB-\\uDDEE\\uDDF0-\\uDDF5\\uDDF7\\uDDFA-\\uDDFF]|\\uD83C\\uDDE9\\uD83C[\\uDDEA\\uDDEC\\uDDEF\\uDDF0\\uDDF2\\uDDF4\\uDDFF]|\\uD83C\\uDDEA\\uD83C[\\uDDE6\\uDDE8\\uDDEA\\uDDEC\\uDDED\\uDDF7-\\uDDFA]|\\uD83C\\uDDEB\\uD83C[\\uDDEE-\\uDDF0\\uDDF2\\uDDF4\\uDDF7]|\\uD83C\\uDDEC\\uD83C[\\uDDE6\\uDDE7\\uDDE9-\\uDDEE\\uDDF1-\\uDDF3\\uDDF5-\\uDDFA\\uDDFC\\uDDFE]|\\uD83C\\uDDED\\uD83C[\\uDDF0\\uDDF2\\uDDF3\\uDDF7\\uDDF9\\uDDFA]|\\uD83C\\uDDEE\\uD83C[\\uDDE8-\\uDDEA\\uDDF1-\\uDDF4\\uDDF6-\\uDDF9]|\\uD83C\\uDDEF\\uD83C[\\uDDEA\\uDDF2\\uDDF4\\uDDF5]|\\uD83C\\uDDF0\\uD83C[\\uDDEA\\uDDEC-\\uDDEE\\uDDF2\\uDDF3\\uDDF5\\uDDF7\\uDDFC\\uDDFE\\uDDFF]|\\uD83C\\uDDF1\\uD83C[\\uDDE6-\\uDDE8\\uDDEE\\uDDF0\\uDDF7-\\uDDFB\\uDDFE]|\\uD83C\\uDDF2\\uD83C[\\uDDE6\\uDDE8-\\uDDED\\uDDF0-\\uDDFF]|\\uD83C\\uDDF3\\uD83C[\\uDDE6\\uDDE8\\uDDEA-\\uDDEC\\uDDEE\\uDDF1\\uDDF4\\uDDF5\\uDDF7\\uDDFA\\uDDFF]|\\uD83C\\uDDF4\\uD83C\\uDDF2|\\uD83C\\uDDF5\\uD83C[\\uDDE6\\uDDEA-\\uDDED\\uDDF0-\\uDDF3\\uDDF7-\\uDDF9\\uDDFC\\uDDFE]|\\uD83C\\uDDF6\\uD83C\\uDDE6|\\uD83C\\uDDF7\\uD83C[\\uDDEA\\uDDF4\\uDDF8\\uDDFA\\uDDFC]|\\uD83C\\uDDF8\\uD83C[\\uDDE6-\\uDDEA\\uDDEC-\\uDDF4\\uDDF7-\\uDDF9\\uDDFB\\uDDFD-\\uDDFF]|\\uD83C\\uDDF9\\uD83C[\\uDDE6\\uDDE8\\uDDE9\\uDDEB-\\uDDED\\uDDEF-\\uDDF4\\uDDF7\\uDDF9\\uDDFB\\uDDFC\\uDDFF]|\\uD83C\\uDDFA\\uD83C[\\uDDE6\\uDDEC\\uDDF2\\uDDF3\\uDDF8\\uDDFE\\uDDFF]|\\uD83C\\uDDFB\\uD83C[\\uDDE6\\uDDE8\\uDDEA\\uDDEC\\uDDEE\\uDDF3\\uDDFA]|\\uD83C\\uDDFC\\uD83C[\\uDDEB\\uDDF8]|\\uD83C\\uDDFD\\uD83C\\uDDF0|\\uD83C\\uDDFE\\uD83C[\\uDDEA\\uDDF9]|\\uD83C\\uDDFF\\uD83C[\\uDDE6\\uDDF2\\uDDFC]|\\uD83C[\\uDCCF\\uDD8E\\uDD91-\\uDD9A\\uDDE6-\\uDDFF\\uDE01\\uDE32-\\uDE36\\uDE38-\\uDE3A\\uDE50\\uDE51\\uDF00-\\uDF20\\uDF2D-\\uDF35\\uDF37-\\uDF7C\\uDF7E-\\uDF84\\uDF86-\\uDF93\\uDFA0-\\uDFC1\\uDFC5\\uDFC6\\uDFC8\\uDFC9\\uDFCF-\\uDFD3\\uDFE0-\\uDFF0\\uDFF4\\uDFF8-\\uDFFF]|\\uD83D[\\uDC00-\\uDC3E\\uDC40\\uDC44\\uDC45\\uDC51-\\uDC65\\uDC6A-\\uDC6D\\uDC6F\\uDC79-\\uDC7B\\uDC7D-\\uDC80\\uDC84\\uDC88-\\uDCA9\\uDCAB-\\uDCFC\\uDCFF-\\uDD3D\\uDD4B-\\uDD4E\\uDD50-\\uDD67\\uDDA4\\uDDFB-\\uDE44\\uDE48-\\uDE4A\\uDE80-\\uDEA2\\uDEA4-\\uDEB3\\uDEB7-\\uDEBF\\uDEC1-\\uDEC5\\uDED0-\\uDED2\\uDED5\\uDEEB\\uDEEC\\uDEF4-\\uDEFA\\uDFE0-\\uDFEB]|\\uD83E[\\uDD0D\\uDD0E\\uDD10-\\uDD17\\uDD1D\\uDD20-\\uDD25\\uDD27-\\uDD2F\\uDD3A\\uDD3C\\uDD3F-\\uDD45\\uDD47-\\uDD71\\uDD73-\\uDD76\\uDD7A-\\uDDA2\\uDDA5-\\uDDAA\\uDDAE-\\uDDB4\\uDDB7\\uDDBA\\uDDBC-\\uDDCA\\uDDD0\\uDDDE-\\uDDFF\\uDE70-\\uDE73\\uDE78-\\uDE7A\\uDE80-\\uDE82\\uDE90-\\uDE95]|[\\u23E9-\\u23EC\\u23F0\\u23F3\\u267E\\u26CE\\u2705\\u2728\\u274C\\u274E\\u2753-\\u2755\\u2795-\\u2797\\u27B0\\u27BF\\uE50A])|\\uFE0F",
					RegexOptions.Compiled | RegexOptions.CultureInvariant);

			public static IEnumerable<string> FindEmojis(string str)
			{
				foreach (Match match in EmojiRegex.Matches(str))
				{
					yield return WebParseEmojiRegExMatchEvaluator(match);
				}
			}

			#region FrwTwemoji stuff

			private const char U_200D = '\u200D'; // 8205
			private const char U_FE0_F = '\uFE0F'; // 65039
			private const char U_D83_C = '\uD83C'; // 55356
			private const char U_DFFC = '\uDFFC'; // 57340
			private const char U_DFFD = '\uDFFD'; // 57341
			private const char U_DFFE = '\uDFFE'; // 57342
			private const char U_DFFF = '\uDFFF'; // 57343


			private static int ConvertUtf16ToCodePoint(string utf16)
			{
				var s = utf16.ToCharArray();

				return s.GetUpperBound(0) == 0 ? char.ConvertToUtf32(utf16, 0) : char.ConvertToUtf32(s[0], s[1]);
			}

			private static string WebParseEmojiRegExMatchEvaluator(Capture match)
			{
				var emoji = string.Empty;
				var s = match.Value.ToCharArray();
				var upperboundOfS = s.GetUpperBound(0);
				int codepoint;
				try
				{
					if (upperboundOfS < 2)
					{
						if (upperboundOfS == 1 && s[1] == U_FE0_F)
						{
							codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[0] }));
						}
						else
						{
							codepoint = ConvertUtf16ToCodePoint(match.Value);
						}

						emoji = $"{codepoint:x}".ToUpperInvariant();
					}
					else
					{
						var i = 0;
						while (i <= upperboundOfS)
						{
							if (emoji.Length > 0)
							{
								emoji += "-";
							}

							if (s[i] != U_200D)
							{
								if (i + 1 <= upperboundOfS && s[i + 1] != U_200D)
								{
									if (i + 2 <= upperboundOfS && s[i + 1] == U_D83_C) // XXXX - 55356 - 57343
									{
										codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i] }));
										emoji += $"{codepoint:x}".ToUpperInvariant();
										if (s[i + 2] == U_DFFC) // XXXX - 55356 - 57340
										{
											emoji += "-1F3FC";
										}

										if (s[i + 2] == U_DFFD) // XXXX - 55356 - 57341
										{
											emoji += "-1F3FD";
										}

										if (s[i + 2] == U_DFFE) // XXXX - 55356 - 57342
										{
											emoji += "-1F3FE";
										}

										if (s[i + 2] == U_DFFF) // XXXX - 55356 - 57343
										{
											emoji += "-1F3FF";
										}

										i += 3;
									}
									else
									{
										if (s[i + 1] == U_FE0_F)
										{
											// Issue #10 when there is 2️⃣ in the text : https://github.com/FrenchW/FrwTwemoji/issues/10
											// s[0]: 50
											// s[1]: 65039
											// s[2]: 8419
											codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i + 2] }));
											var codepoint0 = ConvertUtf16ToCodePoint(new string(new[] { s[i] }));
											emoji += $"{codepoint0:x}-".ToUpperInvariant() + $"{codepoint:x}".ToUpperInvariant();
											i += 3;
										}
										else
										{
											if (i + 2 <= upperboundOfS && s[i + 2] == U_FE0_F)
											{
												// Issue when there is {🅰️}	 in the text
												// s[0]: 55356
												// s[1]: 56688
												// s[2]: 65039
												codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i], s[i + 1] }));
												emoji += $"{codepoint:x}".ToUpperInvariant();
												i += 3;
											}
											else
											{
												codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i], s[i + 1] }));
												emoji += $"{codepoint:x}".ToUpperInvariant();
												i += 2;
											}
										}
									}
								}
								else
								{
									codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i], s[i + 1] }));
									emoji += $"{codepoint:x}".ToUpperInvariant();
									i += 1;
								}
							}
							else
							{
								if (i + 2 <= upperboundOfS && s[i + 2] == U_FE0_F)
								{
									codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i + 1] }));
									emoji += "200D-" + $"{codepoint:x}".ToUpperInvariant() + "-FE0F";
									i += 3;
								}
								else
								{
									if (i + 2 <= upperboundOfS && s[i + 2] != U_200D)
									{
										codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i + 1], s[i + 2] }));
										emoji += "200D-" + $"{codepoint:x}".ToUpperInvariant();
										i += 3;
									}
									else
									{
										codepoint = ConvertUtf16ToCodePoint(new string(new[] { s[i + 1] }));
										emoji += "200D-" + $"{codepoint:x}".ToUpperInvariant();
										i += 2;
									}
								}
							}
						}
					}
				}
				catch
				{
					codepoint = ConvertUtf16ToCodePoint("🆘");
					emoji = $"{codepoint:x}".ToUpperInvariant();
				}

				return emoji;
			}

			#endregion
		}

		#endregion
	}
}