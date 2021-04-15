namespace CatCore.Services.Interfaces
{
	public interface IPlatformService
	{
		internal void Start();
		internal void Stop();

		public IChatService GetChatService();
	}
}