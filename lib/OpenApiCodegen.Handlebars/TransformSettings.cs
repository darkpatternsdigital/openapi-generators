using DarkPatterns.Json.Documents;
using DarkPatterns.OpenApi.Abstractions;
using System.Linq;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen.Handlebars.Templates;
using System;

namespace DarkPatterns.OpenApiCodegen.Handlebars;

/// <param name="SourceBaseUri">The directory that source comments in generated code are relative to; see <see cref="ToSourceReference"/>.</param>
public record TransformSettings(SchemaRegistry SchemaRegistry, string CodeGeneratorVersionInfo, Uri? SourceBaseUri = null)
{
	public static CompositeOpenApiSourceProvider BuildComposite(
		SchemaRegistry schemaRegistry,
		string versionInfo,
		System.Func<TransformSettings, ISourceProvider>[] factories,
		Uri? sourceBaseUri = null)
	{
		var settings = new TransformSettings(schemaRegistry, versionInfo, sourceBaseUri);

		return new CompositeOpenApiSourceProvider(
			factories.Select(factory => factory(settings)).ToArray()
		);
	}

	/// <summary>
	/// Identifies a schema node for source comments: file URIs become relative to
	/// <see cref="SourceBaseUri"/> (e.g. <c>../../schemas/pets.yaml#/components/schemas/Pet</c>), so
	/// generated code doesn't contain machine paths. Other URIs, such as a <c>SchemaId</c>, are kept as written.
	/// </summary>
	public string ToSourceReference(Uri id)
	{
		if (SourceBaseUri is not { IsAbsoluteUri: true, IsFile: true } baseUri || !id.IsAbsoluteUri || !id.IsFile)
			return id.OriginalString;
		// Round-trip through AbsoluteUri to normalize equivalent forms, such as file://C:/ and file:///C:/
		var relative = baseUri.MakeRelativeUri(new Uri(id.AbsoluteUri));
		return relative.IsAbsoluteUri ? id.OriginalString : Uri.UnescapeDataString(relative.OriginalString);
	}

	public PartialHeader Header(Uri id)
	{
		if (!SchemaRegistry.DocumentRegistry.TryGetDocument(id, out var doc))
		{
			return new PartialHeader(null, null, CodeGeneratorVersionInfo);
		}
		var info = doc.Dialect.GetInfo(doc);
		return Header(info.Title, info.Description);
	}

	public PartialHeader Header(string? title, string? description = null)
	{
		return new PartialHeader(
			AppTitle: title,
			AppDescription: description,
			CodeGeneratorVersionInfo: CodeGeneratorVersionInfo
		);
	}
}
