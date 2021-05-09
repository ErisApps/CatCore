using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IPlatformService : IChatService
	{
		internal void Start();
		internal void Stop();

		public IChatChannel? DefaultChannel { get; }
	}
}