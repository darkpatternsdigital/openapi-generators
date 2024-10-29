// https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Index.cs
// https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Range.cs

#if !NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER

namespace System.Collections.Generic;

internal static class KeyValuePairExtensions
{
	public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
	{
		key = tuple.Key;
		value = tuple.Value;
	}
}

#endif
