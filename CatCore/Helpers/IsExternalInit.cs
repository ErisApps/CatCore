namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Polyfill for IsExternalInit to support init accessors in .NET Standard 2.0+
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	internal sealed class IsExternalInit : System.Attribute
	{
	}
}

