using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DarkPatterns.Json.Documents;

public static class DocumentRegistryExtensions
{
	public static bool TryGetDocumentSettings<T>(this DocumentRegistry registry, Uri documentUri, [NotNullWhen(true)] out T? settings)
		where T : class
	{
		if (!registry.TryGetDocument(documentUri, out var document))
		{
			settings = null;
			return false;
		}

		settings = document.Settings.SettingObjects.OfType<T>().FirstOrDefault();
		return settings != null;
	}
}
