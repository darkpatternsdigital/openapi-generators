
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using DarkPatterns.OpenApi.CSharp.Templates;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Metadata;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using DarkPatterns.OpenApi.Specifications.v3_0;
using DarkPatterns.OpenApiCodegen;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.CSharp;

public class CSharpSchemaSourceProvider(
	TransformSettings settings,
	CSharpSchemaOptions options,
	HandlebarsFactory? handlebarsFactory = null
) : SchemaSourceProvider(settings.SchemaRegistry)
{
	private readonly HandlebarsFactory handlebarsFactory = handlebarsFactory ?? HandlebarsFactoryDefaults.Default;
	private readonly CSharpInlineSchemas inlineSchemas = new CSharpInlineSchemas(options, settings.SchemaRegistry.DocumentRegistry);

	protected override SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic)
	{
		if (!inlineSchemas.ProduceSourceEntry(entry)) return null;
		var targetNamespace = options.GetNamespace(entry);
		var className = GetClassName(entry);

		Templates.Model? model = GetModel(entry, diagnostic, className);
		if (model == null)
			return null;
		var sourceText = CSharpHandlebarsCommon.ProcessModel(
			header: settings.Header,
			packageName: targetNamespace,
			schemaId: entry.Metadata.Id,
			model: model,
			handlebarsFactory.Handlebars
		);
		return new SourceEntry(
			Key: $"{targetNamespace}.{className}.cs",
			SourceText: sourceText
		);
	}

	private Templates.Model? GetModel(JsonSchema entry, OpenApiTransformDiagnostic diagnostic, string className)
	{
		if (entry.TryGetAnnotation<EnumKeyword>() != null
			&& entry.TryGetAnnotation<TypeKeyword>() is { Value: TypeKeyword.Common.String })
			return ToEnumModel(className, entry);
		if (entry.TryGetAnnotation<OneOfKeyword>() != null)
			return ToOneOfModel(className, entry);
		if (BuildObjectModel(entry) is ObjectModel model)
			return ToObjectModel(className, entry, model, diagnostic);
		return null;
	}

	private ObjectModel? BuildObjectModel(JsonSchema schema)
	{
		if (schema.TryGetAnnotation<AllOfKeyword>() is AllOfKeyword allOf && allOf.Schemas.Count > 0)
		{
			var models = allOf.Schemas.Select(BuildObjectModel).ToArray();
			if (!models.All(v => v != null)) return null;
			return new ObjectModel(
				Properties: () => models.SelectMany(m => m!.Properties()).Aggregate(new Dictionary<string, JsonSchema>(), (prev, kvp) =>
				{
					prev[kvp.Key] = kvp.Value;
					return prev;
				}),
				Required: () => models.SelectMany(m => m!.Required()).Distinct(),
				LegacyOptionalBehavior: models.Any(m => m!.LegacyOptionalBehavior)
			);
		}

		if (schema.TryGetAnnotation<TypeKeyword>() is { Value: "object" } ||
			schema.TryGetAnnotation<PropertiesKeyword>() != null)
			return new ObjectModel(
				Properties: () => schema.TryGetAnnotation<PropertiesKeyword>()?.Properties ?? new Dictionary<string, JsonSchema>(),
				Required: () => schema.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties ?? Enumerable.Empty<string>(),
				LegacyOptionalBehavior: schema.UseOptionalAsNullable()
			);

		return null;
	}

	private string GetClassName(JsonSchema schema) =>
		options.ToClassName(schema, inlineSchemas.UriToClassIdentifier(schema.Metadata.Id));

	record ObjectModel(Func<IReadOnlyDictionary<string, JsonSchema>> Properties, Func<IEnumerable<string>> Required, bool LegacyOptionalBehavior);

	private Templates.ObjectModel ToObjectModel(string className, JsonSchema schema, ObjectModel objectModel, OpenApiTransformDiagnostic diagnostic)
	{
		if (objectModel == null)
			throw new ArgumentNullException(nameof(objectModel));
		var properties = objectModel.Properties();
		var required = new HashSet<string>(objectModel.Required());

		Templates.ModelVar[] vars = (from entry in properties
									 let req = required.Contains(entry.Key)
									 let dataType = inlineSchemas.ToInlineDataType(entry.Value)
									 let resolved = objectModel.LegacyOptionalBehavior && !req ? dataType.MakeNullable() : dataType
									 select new Templates.ModelVar(
										 BaseName: entry.Key,
										 DataType: resolved.Text,
										 Nullable: resolved.Nullable,
										 IsContainer: resolved.IsEnumerable,
										 Name: CSharpNaming.ToPropertyName(entry.Key, options.ReservedIdentifiers("object", className)),
										 Required: req,
										 Optional: !req && !objectModel.LegacyOptionalBehavior,
										 Pattern: entry.Value?.TryGetAnnotation<PatternKeyword>()?.Pattern,
										 MinLength: entry.Value?.TryGetAnnotation<MinLengthKeyword>()?.Value,
										 MaxLength: entry.Value?.TryGetAnnotation<MaxLengthKeyword>()?.Value,
										 Minimum: entry.Value?.TryGetAnnotation<MinimumKeyword>()?.Value,
										 Maximum: entry.Value?.TryGetAnnotation<MaximumKeyword>()?.Value
									 )).ToArray();

		return new Templates.ObjectModel(
			Description: schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			ClassName: className,
			Parent: null, // TODO - if "all of" and only one was a reference, we should be able to use inheritance.
			Vars: vars.ToArray()
		);
	}

	private Templates.EnumModel ToEnumModel(string className, JsonSchema schema)
	{
		return new Templates.EnumModel(
			schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			className,
			IsString: schema.TryGetAnnotation<TypeKeyword>()?.Value == TypeKeyword.Common.String,
			EnumVars: (from entry in schema.TryGetAnnotation<EnumKeyword>()?.Values
					   select entry switch
					   {
						   JsonValue val when val.TryGetValue<string>(out var name) => new Templates.EnumVar(CSharpNaming.ToPropertyName(name, options.ReservedIdentifiers("enum", className)), name),
						   _ => throw new NotSupportedException()
					   }).ToArray()
		);
	}

	private Templates.TypeUnionModel ToOneOfModel(string className, JsonSchema schema)
	{
		var discriminator = schema.TryGetAnnotation<DiscriminatorKeyword>();
		var oneOf = schema.TryGetAnnotation<OneOfKeyword>()!;

		return new Templates.TypeUnionModel(
			schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			className,
			AllowAnyOf: false,
			DiscriminatorProperty: discriminator?.PropertyName,
			TypeEntries: oneOf.Schemas
				.Select((e, index) =>
				{
					var id = e.Metadata.Id;
					string? discriminatorValue = e.GetLastContextPart();
					if (discriminator?.Mapping?.FirstOrDefault(kvp => kvp.Value.OriginalString == id.OriginalString) is { Key: string key, Value: var relativeId })
					{
						discriminatorValue = key;
						id = new Uri(id, relativeId);
					}
					return new TypeUnionEntry(
						TypeName: inlineSchemas.ToInlineDataType(e).Text,
						Identifier: CSharpNaming.ToPropertyName(discriminatorValue ?? inlineSchemas.UriToClassIdentifier(id), options.ReservedIdentifiers("object", className)),
						DiscriminatorValue: discriminatorValue
					);
				}).ToArray()
		);
	}

}
