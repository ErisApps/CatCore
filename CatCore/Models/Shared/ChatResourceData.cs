namespace CatCore.Models.Shared
{
	public readonly struct ChatResourceData
	{
		public string Id { get; }
		public string Name { get; }
		public string Uri { get; }
		public bool IsAnimated { get; }
		public string Type { get; }

		public ChatResourceData(string id, string name, string uri, bool isAnimated, string type)
		{
			Id = id;
			Name = name;
			Uri = uri;
			IsAnimated = isAnimated;
			Type = type;
		}
	}
}