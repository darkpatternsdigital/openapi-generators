namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

public static class OptionalExtensions
{
	public static bool TryGet<T>(this IOptional<T>? input, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T value)
	{
		if (input is Optional<T>.Present { Value: var presentValue })
		{
			value = presentValue;
			return true;
		}
		value = default!;
		return false;
	}

	public static T? GetValueOrDefault<T>(this IOptional<T>? input) =>
		input.TryGet(out var result) ? result : default;
	public static T GetValueOrDefault<T>(this IOptional<T>? input, T defaultValue) =>
		input.TryGet(out var result) ? result : defaultValue;
}
