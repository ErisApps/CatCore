using System.Threading.Tasks;

namespace CatCore.Services.Interfaces
{
	internal interface IInitializable
	{
		Task Initialize();
	}
}