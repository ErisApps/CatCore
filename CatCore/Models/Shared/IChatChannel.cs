using System;

namespace CatCore.Models.Shared
{
	public interface IChatChannel : ICloneable
	{
		string Id { get; }
		string Name { get; }
	}
}