namespace CatCore.Models.Shared
{
	public class Emoji : IChatEmote
	{
		public string Id { get; }
		public string Name { get; }
		public int StartIndex { get; }
		public int EndIndex { get; }
		public string Url { get; }
		public bool Animated => false;

		public Emoji(string id, string name, int startIndex, int endIndex, string url)
		{
			Id = "Emoji_" + id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
		}
	}
}