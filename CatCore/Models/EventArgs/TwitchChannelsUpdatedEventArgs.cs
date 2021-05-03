using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CatCore.Models.EventArgs
{
	public class TwitchChannelsUpdatedEventArgs : System.EventArgs
	{
		public TwitchChannelsUpdatedEventArgs(IDictionary<string, string> enabledChannels, IDictionary<string, string> disabledChannels)
		{
			EnabledChannels = new ReadOnlyDictionary<string, string>(enabledChannels);
			DisabledChannels = new ReadOnlyDictionary<string, string>(disabledChannels);
		}

		public readonly ReadOnlyDictionary<string, string> EnabledChannels;
		public readonly ReadOnlyDictionary<string, string> DisabledChannels;
	}
}