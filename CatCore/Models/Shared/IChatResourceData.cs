namespace CatCore.Models.Shared
{
	public interface IChatResourceData
	{
		string Id { get; }
		string Name { get; }
		string Url { get; }
		bool IsAnimated { get; }
		string Type { get; }
	}
}