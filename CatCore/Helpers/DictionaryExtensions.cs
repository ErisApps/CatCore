using System.Collections.Generic;
using System.Text;

namespace CatCore.Helpers
{
	internal static class DictionaryExtensions
	{
		public static string ToPrettyString<TKey, TValue>(this IDictionary<TKey, TValue> dict)
		{
			var str = new StringBuilder();
			str.Append("{ ");
			foreach (var pair in dict)
			{
				str.Append($"{pair.Key}={pair.Value}; ");
			}
			str.Append('}');
			return str.ToString();
		}
	}
}