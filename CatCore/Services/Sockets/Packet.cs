using System.Text.Json;

namespace CatCore.Services.Sockets
{
	/// <summary>
	/// Abstract class to define packet types
	/// </summary>
	public abstract class Packet
	{
		public string ToJson()
		{
			return JsonSerializer.Serialize(this);
		}

		public static T? FromJson<T>(string json) where T : Packet
		{
			return JsonSerializer.Deserialize<T>(json);
		}
	}
}