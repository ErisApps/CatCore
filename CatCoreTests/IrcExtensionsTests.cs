using System.Collections.Generic;
using System.Collections.ObjectModel;
using CatCore.Helpers;
using FluentAssertions;
using Xunit;

namespace CatCoreTests
{
	public class IrcExtensionsTests
	{
		public static IEnumerable<object[]> ParseIrcMessageData => new[]
		{
			new object[] {":tmi.twitch.tv 376 realeris :>", null!, "tmi.twitch.tv", "376", "realeris", ">"},
			new object[] {":realeris!realeris@realeris.tmi.twitch.tv JOIN #realeris", null!, "realeris!realeris@realeris.tmi.twitch.tv", "JOIN", "realeris", null!},
			new object[] {":realeris.tmi.twitch.tv 353 realeris = #realeris :realeris", null!, "realeris.tmi.twitch.tv", "353", "realeris = #realeris", "realeris"},
			new object[] {":realeris.tmi.twitch.tv 366 realeris #realeris :End of /NAMES list", null!, "realeris.tmi.twitch.tv", "366", "realeris #realeris", "End of /NAMES list"},
			new object[]
			{
				":tmi.twitch.tv CAP * ACK :twitch.tv/tags twitch.tv/commands twitch.tv/membership", null!, "tmi.twitch.tv", "CAP", "* ACK", "twitch.tv/tags twitch.tv/commands twitch.tv/membership"
			},
			new object[]
			{
				"@badge-info=subscriber/1;badges=broadcaster/1,subscriber/0;client-nonce=1ef9899702c12a2081fa33899d7e8465;color=#FF69B4;display-name=RealEris;emotes=;flags=;id=b4595e1c-dd1b-4e45-b7df-a3403c945ad6;mod=0;room-id=405499635;subscriber=1;tmi-sent-ts=1614390981294;turbo=0;user-id=405499635;user-type= :realeris!realeris@realeris.tmi.twitch.tv PRIVMSG #realeris :Heya",
				// The tags "emotes", "flags" and "user-type" will are omitted out as their value was missing
				new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
				{
					{"badge-info", "subscriber/1"},
					{"badges", "broadcaster/1,subscriber/0"},
					{"client-nonce", "1ef9899702c12a2081fa33899d7e8465"},
					{"color", "#FF69B4"},
					{"display-name", "RealEris"},
					{"id", "b4595e1c-dd1b-4e45-b7df-a3403c945ad6"},
					{"mod", "0"},
					{"room-id", "405499635"},
					{"subscriber", "1"},
					{"tmi-sent-ts", "1614390981294"},
					{"turbo", "0"},
					{"user-id", "405499635"},
				}),
				"realeris!realeris@realeris.tmi.twitch.tv",
				"PRIVMSG",
				"realeris",
				"Heya"
			},
			new object[]
			{
				"@msg-id=slow_off :tmi.twitch.tv NOTICE #realeris :This room is no longer in slow mode.",
				new ReadOnlyDictionary<string, string>(new Dictionary<string, string> {{"msg-id", "slow_off"}}),
				"tmi.twitch.tv",
				"NOTICE",
				"realeris",
				"This room is no longer in slow mode."
			},
			new object[]
			{
				"@emote-only=0;followers-only=-1;r9k=0;rituals=0;room-id=502843244;slow=0;subs-only=0 :tmi.twitch.tv ROOMSTATE #pinkwastaken",
				new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
				{
					{"emote-only", "0"},
					{"followers-only", "-1"},
					{"r9k", "0"},
					{"rituals", "0"},
					{"room-id", "502843244"},
					{"slow", "0"},
					{"subs-only", "0"}
				}),
				"tmi.twitch.tv",
				"ROOMSTATE",
				"pinkwastaken",
				null!
			}
		};

		[Theory]
		[MemberData(nameof(ParseIrcMessageData))]
		public void ParseIrcMessageTests(string inputData, ReadOnlyDictionary<string, string>? expectedTags, string? expectedPrefix, string expectedCommandType, string? expectedChannelName,
			string? expectedMessage)
		{
			// Arrange
			// NOP

			// Act
			IrcExtensions.ParseIrcMessage(inputData, out var tags, out var prefix, out var commandType, out var channelName, out var message);

			// Assert
			tags.Should().BeEquivalentTo(expectedTags);
			prefix.Should().Be(expectedPrefix);
			commandType.Should().Be(expectedCommandType);
			channelName.Should().Be(expectedChannelName);
			message.Should().Be(expectedMessage);
		}
	}
}