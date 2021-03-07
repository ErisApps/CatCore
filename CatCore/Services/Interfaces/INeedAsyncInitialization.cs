using System.Threading.Tasks;

namespace CatCore.Services.Interfaces
{
	internal interface INeedAsyncInitialization
	{
		Task Initialize();
	}
}