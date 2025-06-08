using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications.Dialects;

namespace DarkPatterns.Json.Specifications;

public static class DialectSettingsExtensions
{
	public static void SetDialect(this DocumentSettings settings, IJsonSchemaDialect dialect)
	{
		settings.SettingObjects.Add(dialect);
	}

	public static IJsonSchemaDialect GetDialect(this DocumentSettings settings)
	{
		return settings.SettingObjects.OfType<IJsonSchemaDialect>().FirstOrDefault()
			?? StandardDialects.CoreNext;
	}
}
