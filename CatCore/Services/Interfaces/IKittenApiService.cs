namespace CatCore.Services.Interfaces
{
	internal interface IKittenApiService : INeedAsyncInitialization
	{
		string ServerUri { get; }
	}
}