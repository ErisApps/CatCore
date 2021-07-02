using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CatCore.Services.Sockets.Packets
{
	/// <summary>
	/// Abstract class to define packet types
	/// </summary>
	public abstract class Packet
	{
		// Force packet name to be compile time constant
		public string PacketName => GetType().Name;


		/// This is probably overkill but WHO CARES? WHY WOULD ANYONE CARE BECAUSE IT'S ASYNC SO IT MUST BE FAST!
		/// FAST LIKE FAST AND FURIOUS! IDK I NEVER WATCHED THE MOVIES
		public static async Task<Packet?> GetPacketFromJson(string json)
		{
			if (string.IsNullOrEmpty(json))
			{
				return null;
			}

			return await Task.Run((() =>
			{
				var jsonNode = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

				if (jsonNode == null)
				{
					return null;
				}

				if (!jsonNode.TryGetValue(nameof(PacketName), out var packetNameElm))
				{
					return null;
				}

				var packetName = packetNameElm.GetString();

				if (packetName == null)
				{
					return null;
				}

				var type = GetPacketTypeByName(packetName!);

				return type == null ? null : JsonSerializer.Deserialize(json, type!) as Packet;
			}));
		}

		private static Type? GetPacketTypeByName(string name)
		{
			var type = Type.GetType($"{typeof(Packet).Namespace}.{name}");

			if (type == null || type == typeof(Packet) || !typeof(Packet).IsAssignableFrom(type.BaseType))
			{
				return null;
			}

			return type;
		}
	}

	public class GetHello : Packet
	{
		[JsonConstructor]
		private GetHello(string hello)
		{
			this.Hello = hello;
		}

		public string Hello { get; }
	}

	public class RespondHello : Packet
	{
		[JsonConstructor]
		public RespondHello(string helloToSend)
		{
			HelloBack = helloToSend;
		}

		[JsonInclude]
		public string HelloBack { get; }
	}
}