
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Diagnostics;
using DarkPatterns.Json.Specifications;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Applicator;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Metadata;
using DarkPatterns.Json.Specifications.Keywords.Draft2020_12Validation;
using DarkPatterns.OpenApi.Abstractions;
using DarkPatterns.OpenApi.Transformations;
using DarkPatterns.OpenApiCodegen;
using DarkPatterns.OpenApiCodegen.Handlebars;

namespace DarkPatterns.OpenApi.TypeScript;

public class TypeScriptSchemaSourceProvider(
	TransformSettings settings,
	TypeScriptSchemaOptions options,
	HandlebarsFactory? handlebarsFactory = null
) : SchemaSourceProvider(settings.SchemaRegistry)
{
	private readonly HandlebarsFactory handlebarsFactory = handlebarsFactory ?? HandlebarsFactoryDefaults.Default;
	private readonly TypeScriptInlineSchemas inlineSchemas = new TypeScriptInlineSchemas(options, settings.SchemaRegistry.DocumentRegistry);

	protected override SourcesResult GetAdditionalSources()
	{
		var exportStatements = inlineSchemas.GetExportStatements(settings.SchemaRegistry.GetSchemas(), options, options.SchemasFolder).ToArray();
		if (exportStatements.Length > 0)
			return new([new SourceEntry(
				Key: System.IO.Path.Combine(options.SchemasFolder, "index.ts"),
				SourceText: TypeScriptHandlebarsCommon.ProcessModelBarrelFile(
					new Templates.ModelBarrelFile(new OpenApiCodegen.Handlebars.Templates.PartialHeader(
						"All models",
						null,
						settings.CodeGeneratorVersionInfo
					), exportStatements),
					handlebarsFactory.Handlebars
				)
			)], []);
		return SourcesResult.Empty;
	}

	protected override SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic)
	{
		if (!inlineSchemas.ProduceSourceEntry(entry)) return null;
		return TransformSchema(entry, diagnostic);
	}

	public SourceEntry? TransformSchema(JsonSchema schema, OpenApiTransformDiagnostic diagnostic)
	{
		var className = UseReferenceName(schema);

		var typeInfo = TypeScriptTypeInfo.From(schema);
		Templates.Model? model = typeInfo switch
		{
			{ TypeAnnotation: { AllowsArray: true } } or { Items: JsonSchema _ } => ToArrayModel(className, typeInfo),
			// TODO: strings aren't the only enum values
			{ Enum.Count: > 0, TypeAnnotation: { AllowsString: true } } => ToEnumModel(className, typeInfo),
			{ OneOf.Count: > 0 } => ToOneOfModel(className, typeInfo),
			_ => BuildObjectModel(typeInfo.Info) switch
			{
				ObjectModel objectModel => ToObjectModel(className, typeInfo.Info, objectModel, diagnostic)(),
				_ => ToInlineSchemaModel(className, typeInfo.Info, diagnostic),
			}
		};
		if (model == null)
		{
			diagnostic.Diagnostics.Add(new UnableToGenerateSchema(className, settings.SchemaRegistry.DocumentRegistry.ResolveLocation(schema.Metadata)));
			return null;
		}
		var entry = TypeScriptHandlebarsCommon.ProcessModel(
			header: settings.Header(schema.Metadata.Id),
			packageName: "",
			model: model,
			handlebarsFactory.Handlebars
		);
		return new SourceEntry(
			Key: ToSourceEntryKey(schema),
			SourceText: entry
		);
	}

	public string UseReferenceName(JsonSchema schema)
	{
		return TypeScriptNaming.ToClassName(inlineSchemas.UriToClassIdentifier(schema.Metadata.Id), options.ReservedIdentifiers());
	}

	public string ToSourceEntryKey(JsonSchema schema)
	{
		var className = UseReferenceName(schema);
		return Path.Combine(options.SchemasFolder, $"{className}.ts");
	}

	private Templates.ArrayModel ToArrayModel(string className, TypeScriptTypeInfo schema)
	{
		var dataType = inlineSchemas.ToInlineDataType(schema.Items);
		return new Templates.ArrayModel(
			schema.Description,
			className,
			Item: dataType.Text,
			Imports: inlineSchemas.GetImportStatements([schema.Items], [schema.Info.EffectiveSchema], options.SchemasFolder).ToArray()
		);
	}

	private Templates.EnumModel ToEnumModel(string className, TypeScriptTypeInfo schema)
	{
		return new Templates.EnumModel(
			schema.Description,
			className,
			TypeScriptNaming.ToPropertyName(className, options.ReservedIdentifiers()),
			// TODO: allow mixed-type enum models
			IsString: schema.TypeAnnotation is { AllowsString: true },
			EnumVars: (from entry in schema.Enum
					   select new Templates.EnumVar(PrimitiveToJsonValue.GetPrimitiveValue(entry))).ToArray()
		);
	}

	private Templates.TypeUnionModel ToOneOfModel(string className, TypeScriptTypeInfo schema)
	{
		var discriminator = schema.Info.TryGetAnnotation<Specifications.v3_0.DiscriminatorKeyword>();
		return new Templates.TypeUnionModel(
			Imports: inlineSchemas.GetImportStatements(schema.OneOf ?? [], [], options.SchemasFolder).ToArray(),
			Description: schema.Description,
			ClassName: className,
			AllowAnyOf: false,
			DiscriminatorProperty: discriminator?.PropertyName,
			TypeEntries: schema.OneOf
				.Select((e, index) =>
				{
					var s = e.ResolveSchemaInfo();
					var id = s.EffectiveSchema.Metadata.Id;
					string? discriminatorValue = e.GetLastContextPart();
					if (discriminator?.Mapping?.FirstOrDefault(
							kvp => new Uri(schema.Info.EffectiveSchema.Metadata.Id, kvp.Value).OriginalString == id.OriginalString
						) is { Key: string key, Value: var relativeId })
					{
						discriminatorValue = key;
						id = new Uri(id, relativeId);
					}

					return new Templates.TypeUnionEntry(
						TypeName: inlineSchemas.ToInlineDataType(e).Text,
						DiscriminatorValue: discriminatorValue
					);
				}).ToArray()
		);
	}

	record ObjectModel(Func<IReadOnlyDictionary<string, JsonSchema>> Properties, Func<IEnumerable<string>> Required, bool LegacyOptionalBehavior);

	private ObjectModel? BuildObjectModel(JsonSchemaInfo schema) =>
		TypeScriptTypeInfo.From(schema) switch
		{
			{ AllOf: { Count: > 0 } allOf } => allOf.Select(s => s.ResolveSchemaInfo()).Select(BuildObjectModel).ToArray() switch
			{
				ObjectModel[] models when models.All(v => v != null) =>
					new ObjectModel(
						Properties: () => models.SelectMany(m => m!.Properties()).Aggregate(new Dictionary<string, JsonSchema>(), (prev, kvp) =>
						{
							prev[kvp.Key] = kvp.Value;
							return prev;
						}),
						Required: () => models.SelectMany(m => m!.Required()).Distinct(),
						LegacyOptionalBehavior: models.Any(m => m!.LegacyOptionalBehavior)
					),
				_ => null
			},
			{ TypeAnnotation: { AllowsObject: true } } or { Properties: { Count: > 0 } } => new ObjectModel(
				Properties: () => schema.TryGetAnnotation<PropertiesKeyword>()?.Properties ?? new Dictionary<string, JsonSchema>(),
				Required: () => schema.TryGetAnnotation<RequiredKeyword>()?.RequiredProperties ?? Enumerable.Empty<string>(),
				LegacyOptionalBehavior: schema.UseOptionalAsNullable()
			),
			_ => null,
		};

	private Func<Templates.ObjectModel> ToObjectModel(string className, JsonSchemaInfo schema, ObjectModel objectModel, OpenApiTransformDiagnostic diagnostic)
	{
		if (objectModel == null)
			throw new ArgumentNullException(nameof(objectModel));
		var properties = objectModel.Properties();
		var required = new HashSet<string>(objectModel.Required());

		Func<Templates.ModelVar>[] vars = (from entry in properties
										   let req = required.Contains(entry.Key)
										   let dataType = inlineSchemas.ToInlineDataType(entry.Value)
										   let resolved = objectModel.LegacyOptionalBehavior && !req ? dataType.MakeNullable() : dataType
										   select (Func<Templates.ModelVar>)(() => new Templates.ModelVar(
											   BaseName: entry.Key,
											   DataType: resolved.Text,
											   Nullable: resolved.Nullable,
											   IsContainer: resolved.IsEnumerable,
											   Name: entry.Key,
											   Required: req,
											   Optional: !req
											))).ToArray();

		return () => new Templates.ObjectModel(
			Imports: inlineSchemas.GetImportStatements(properties.Values, [schema.EffectiveSchema], options.SchemasFolder).ToArray(),
			Description: schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			ClassName: className,
			Parent: null, // TODO - if "all of" and only one was a reference, we should be able to use inheritance.
			Vars: vars.Select(v => v()).ToArray()
		);
	}

	private Templates.InlineModel ToInlineSchemaModel(string className, JsonSchemaInfo schema, OpenApiTransformDiagnostic diagnostic)
	{
		var inlineDefinition = inlineSchemas.GetInlineDataType(schema.EffectiveSchema);
		return new Templates.InlineModel(
			Imports: inlineSchemas.ToImportStatements(inlineDefinition.Imports, [schema.EffectiveSchema], options.SchemasFolder).ToArray(),
			Description: schema.TryGetAnnotation<DescriptionKeyword>()?.Description,
			ClassName: className,
			Content: inlineDefinition.Text
		);
	}
}


public record UnableToGenerateSchema(string schemaName, Location Location) : DiagnosticBase(Location)
{
	public override IReadOnlyList<string> GetTextArguments()
	{
		return [schemaName];
	}
}
