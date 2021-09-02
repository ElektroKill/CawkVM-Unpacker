using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public static class string2 {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => string.IsNullOrEmpty(value);
}
