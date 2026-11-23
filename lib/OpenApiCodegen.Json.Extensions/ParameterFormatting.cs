using System;
using System.ComponentModel;
using System.Globalization;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

/// <summary>
/// Formats values for path, query and header parameters and form fields the way OpenAPI expects,
/// independent of the current culture. Used by generated clients.
/// </summary>
public static class ParameterFormatting
{
	public static string? ToParameterString(object? value) =>
		value switch
		{
			null => null,
			string s => s,
			bool b => b ? "true" : "false",
			byte[] bytes => Convert.ToBase64String(bytes),
			DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
			DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
#if NET6_0_OR_GREATER
			DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
#endif
			// Generated enums carry a TypeConverter that writes the OpenAPI value
			Enum => TypeDescriptor.GetConverter(value.GetType()).ConvertToInvariantString(value),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => TypeDescriptor.GetConverter(value.GetType()).ConvertToInvariantString(value),
		};
}
