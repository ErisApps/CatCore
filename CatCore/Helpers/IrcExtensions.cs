using System;

namespace CatCore.Helpers
{
	internal static class IrcExtensions
	{
		internal static bool ParsePrefix(this string rawPrefix, out bool? isServer, out string? nickname, out string? username, out string? hostname)
		{
			isServer = null;
			nickname = null;
			username = null;
			hostname = null;

			if (rawPrefix.Length == 0)
			{
				return false;
			}

			var rawPrefixSpan = rawPrefix.AsSpan();

			var usernamePosition = rawPrefixSpan.IndexOf('!') + 1;
			var hostnamePosition = rawPrefixSpan.IndexOf('@') + 1;

			if (usernamePosition == 1 || hostnamePosition == 1)
			{
				return false;
			}

			isServer = false;

			if (usernamePosition > 0)
			{
				nickname = rawPrefixSpan.Slice(0, usernamePosition - 1).ToString();
				if (hostnamePosition > 0)
				{
					username = rawPrefixSpan.Slice(usernamePosition, hostnamePosition - usernamePosition - 1).ToString();
					hostname = rawPrefixSpan.Slice(hostnamePosition).ToString();
				}
				else
				{
					username = rawPrefixSpan.Slice(usernamePosition).ToString();
				}
			}
			else if (hostnamePosition > 0)
			{
				nickname = rawPrefixSpan.Slice(0, hostnamePosition - 1).ToString();
				hostname = rawPrefixSpan.Slice(hostnamePosition).ToString();
			}
			else if (rawPrefixSpan.IndexOf('.') > 0)
			{
				hostname = rawPrefix;
				isServer = true;
			}
			else
			{
				nickname = rawPrefix;
			}


			return true;
		}
	}
}