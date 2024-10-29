
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
		var info = entry.ResolveSchemaInfo();
		if (!inlineSchemas.ProduceSourceEntry(info)) return null;
		var targetNamespace = options.GetNamespace(info);
		var className = GetClassName(info);

		Templates.Model? model = GetModel(info, diagnostic, className);
		if (model == null)
			return null;
		var sourceText = CSharpHandlebarsCommon.ProcessModel(
			header: settings.Header(info.EffectiveSchema.Metadata.Id),
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

	private Templates.Model? GetModel(JsonSchemaInfo entry, OpenApiTransformDiagnostic diagnostic, string className)
	{
		if (entry.TryGetAnnotation<EnumKeyword>() != null
			&& entry.TryGetAnnotation<TypeAnnotation>() is { AllowsString: true })
			// TODO: this is not correct - any value could be a enum in OpenAPI
			return ToEnumModel(className, entry);
		if (entry.TryGetAnnotation<OneOfKeyword>() != null)
			return ToOneOfModel(className, entry);
		if (BuildObjectModel(entry) is ObjectModel model)
			return ToObjectModel(className, entry, model, diagnostic);
		return null;
	}

	private ObjectModel? BuildObjectModel(JsonSchemaInfo schema)
	{
		if (schema.TryGetAnnotation<AllOfKeyword>() is AllOfKeyword allOf && allOf.Schemas.Count > 0)
		{
			var models = allOf.Schemas.Select(s => s.ResolveSchemaInfo()).Select(BuildObjectModel).ToArray();
			if (!models.All(v => v != null)) return null;
			return new ObjectModel(
				Properties: () => models.SelectMany(m => m!.Properties()).Aggregate(new Dictionary<string, JsonSchemaInfo>(), (prev, kvp) =>
				{
					prev[kvp.Key] = kvp.Value;
					return prev;
				}),
				Required: () => models.SelectMany(m => m!.Required()).Distinct(),
				LegacyOptionalBehavior: models.Any(m => m!.LegacyOptionalBehavior)
			);
		}

		if (schema.TryGetAnnotation<TypeAnnotation>() is { AllowsObject: true } ||
			schema.TryGetAnnotation<PropertiesKeyword>() != null)
			return new ObjectModel(
				Properties: () => schema.TryGetAnnotation<PropertiesKeyword>()?.Properties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ResolveSchemaInfo()) ?? [],
				Required: () => schema.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties ?? Enumerable.Empty<string>(),
				LegacyOptionalBehavior: schema.UseOptionalAsNullable()
			);

		return null;
	}

	private string GetClassName(JsonSchemaInfo schema) =>
		options.ToClassName(schema, inlineSchemas.UriToClassIdentifier(schema.EffectiveSchema.Metadata.Id));

	record ObjectModel(Func<IReadOnlyDictionary<string, JsonSchemaInfo>> Properties, Func<IEnumerable<string>> Required, bool LegacyOptionalBehavior);

	private Templates.ObjectModel ToObjectModel(string className, JsonSchemaInfo schema, ObjectModel objectModel, OpenApiTransformDiagnostic diagnostic)
	{
		if (objectModel == null)
			throw new ArgumentNullException(nameof(objectModel));
		var properties = objectModel.Properties();
		var required = new HashSet<string>(objectModel.Required());

		Templates.ModelVar[] vars = (from entry in properties
									 let req = required.Contains(entry.Key)
									 let dataType = inlineSchemas.ToInlineDataType(entry.Value.EffectiveSchema)
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

	private Templates.EnumModel ToEnumModel(string className, JsonSchemaInfo schema)
	{
		return new Templates.EnumModel(
			schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			className,
			IsString: schema.TryGetAnnotation<TypeAnnotation>() is { AllowsString: true },
			EnumVars: (from entry in schema.TryGetAnnotation<EnumKeyword>()?.Values
					   select entry switch
					   {
						   JsonValue val when val.TryGetValue<string>(out var name) => new Templates.EnumVar(CSharpNaming.ToPropertyName(name, options.ReservedIdentifiers("enum", className)), name),
						   _ => throw new NotSupportedException()
					   }).ToArray()
		);
	}

	private Templates.TypeUnionModel ToOneOfModel(string className, JsonSchemaInfo schema)
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
					var s = e.ResolveSchemaInfo();
					var id = s.EffectiveSchema.Metadata.Id;
					string? discriminatorValue = s.EffectiveSchema.GetLastContextPart();
					if (discriminator?.Mapping?.FirstOrDefault(
							kvp => new Uri(schema.EffectiveSchema.Metadata.Id, kvp.Value).OriginalString == id.OriginalString
						) is { Key: string key, Value: var relativeId })
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
