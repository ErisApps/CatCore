using System;
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
			},
			new object[]
			{
				@"@badge-info=subscriber/13;badges=subscriber/12;color=#D2691E;display-name=Dav3Schneider;emotes=;flags=;id=042342b6-fb4f-47d4-bf24-b7363abeaa71;login=dav3schneider;mod=0;msg-id=resub;msg-param-cumulative-months=13;msg-param-months=0;msg-param-multimonth-duration=0;msg-param-multimonth-tenure=0;msg-param-should-share-streak=0;msg-param-sub-plan-name=Channel\sSubscription\s(meclipse);msg-param-sub-plan=1000;msg-param-was-gifted=false;room-id=37402112;subscriber=1;system-msg=Dav3Schneider\ssubscribed\sat\sTier\s1.\sThey've\ssubscribed\sfor\s13\smonths!;tmi-sent-ts=1621982140645;user-id=22228110;user-type= :tmi.twitch.tv USERNOTICE #shroud",
				new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
				{
					{"badge-info", "subscriber/13"},
					{"badges","subscriber/12"},
					{"color", "#D2691E"},
					{"display-name", "Dav3Schneider"},
					{"id","042342b6-fb4f-47d4-bf24-b7363abeaa71"},
					{"login","dav3schneider"},
					{"mod","0"},
					{"msg-id","resub"},
					{"msg-param-cumulative-months","13"},
					{"msg-param-months","0"},
					{"msg-param-multimonth-duration","0"},
					{"msg-param-multimonth-tenure","0"},
					{"msg-param-should-share-streak","0"},
					{"msg-param-sub-plan-name",@"Channel\sSubscription\s(meclipse)"},
					{"msg-param-sub-plan","1000"},
					{"msg-param-was-gifted","false"},
					{"room-id","37402112"},
					{"subscriber","1"},
					{"system-msg",@"Dav3Schneider\ssubscribed\sat\sTier\s1.\sThey've\ssubscribed\sfor\s13\smonths!"},
					{"tmi-sent-ts","1621982140645"},
					{"user-id","22228110"}

				}),
				"tmi.twitch.tv",
				"USERNOTICE",
				"shroud",
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
			IrcExtensions.ParseIrcMessage(inputData.AsSpan(), out var tags, out var prefix, out var commandType, out var channelName, out var message);

			// Assert
			tags.Should().BeEquivalentTo(expectedTags);
			prefix.Should().Be(expectedPrefix);
			commandType.Should().Be(expectedCommandType);
			channelName.Should().Be(expectedChannelName);
			message.Should().Be(expectedMessage);
		}

		public static IEnumerable<object[]> ParsePrefixData => new[]
		{
			new object[] { "tmi.twitch.tv", true, true, null!, null!, "tmi.twitch.tv"},
			new object[] { "realeris!realeris@realeris.tmi.twitch.tv", true, false, "realeris", "realeris", "realeris.tmi.twitch.tv"},
			new object[] { "realeris.tmi.twitch.tv", true, true, null!, null!, "realeris.tmi.twitch.tv"}
		};


		[Theory]
		[MemberData(nameof(ParsePrefixData))]
		public void ParsePrefixTests(string inputData, bool expectedCouldParse, bool? expectedIsServer, string? expectedNickname, string? expectedUsername, string? expectedHostname)
		{
			// Arrange
			// NOP

			// Act
			var couldParse = IrcExtensions.ParsePrefix(inputData, out var isServer, out var nickname, out var username, out var hostname);

			// Assert
			couldParse.Should().Be(expectedCouldParse);
			isServer.Should().Be(expectedIsServer);
			nickname.Should().Be(expectedNickname);
			username.Should().Be(expectedUsername);
			hostname.Should().Be(expectedHostname);
		}
	}
}