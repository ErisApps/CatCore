namespace CatCore.Models.Shared
{
	public sealed class ChatResourceData : IChatResourceData
	{
		public string Id { get; }
		public string Name { get; }
		public string Url { get; }
		public bool IsAnimated { get; }
		public string Type { get; }

		public ChatResourceData(string id, string name, string uri, bool isAnimated, string type)
		{
			Id = id;
			Name = name;
			Url = uri;
			IsAnimated = isAnimated;
			Type = type;
		}
	}
}