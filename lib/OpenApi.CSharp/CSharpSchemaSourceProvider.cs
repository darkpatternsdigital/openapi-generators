
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using PrincipleStudios.OpenApi.CSharp.Templates;
using PrincipleStudios.OpenApi.Transformations;
using PrincipleStudios.OpenApi.Transformations.Abstractions;
using PrincipleStudios.OpenApi.Transformations.Specifications;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Applicator;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Metadata;
using PrincipleStudios.OpenApi.Transformations.Specifications.Keywords.Draft2020_12Validation;
using PrincipleStudios.OpenApi.Transformations.Specifications.OpenApi3_0;
using PrincipleStudios.OpenApiCodegen;

namespace PrincipleStudios.OpenApi.CSharp;

public class CSharpSchemaSourceProvider : SchemaSourceProvider
{
	private readonly DocumentRegistry documentRegistry;
	private readonly ISchemaRegistry schemaRegistry;
	private readonly CSharpInlineSchemas inlineSchemas;
	private readonly string baseNamespace;
	private readonly CSharpSchemaOptions options;
	private readonly HandlebarsFactory handlebarsFactory;
	private readonly PartialHeader header;

	public CSharpSchemaSourceProvider(DocumentRegistry documentRegistry, ISchemaRegistry schemaRegistry, string baseNamespace, CSharpSchemaOptions options, HandlebarsFactory handlebarsFactory, Templates.PartialHeader header) : base(schemaRegistry)
	{
		this.documentRegistry = documentRegistry;
		this.schemaRegistry = schemaRegistry;
		this.inlineSchemas = new CSharpInlineSchemas(documentRegistry, options);
		this.baseNamespace = baseNamespace;
		this.options = options;
		this.handlebarsFactory = handlebarsFactory;
		this.header = header;
	}

	protected override SourceEntry? GetSourceEntry(JsonSchema entry, OpenApiTransformDiagnostic diagnostic)
	{
		var targetNamespace = baseNamespace;
		var className = GetClassName(entry);

		Templates.Model? model = GetModel(entry, diagnostic, className);
		if (model == null)
			return null;
		var sourceText = HandlebarsTemplateProcess.ProcessModel(
			header: header,
			packageName: targetNamespace,
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

	private string GetClassName(JsonSchema schema)
	{
		var context = schema.Metadata.Id;
		return CSharpNaming.ToClassName(context.Fragment, options.ReservedIdentifiers());
	}

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
					var id = e.Metadata.Id.OriginalString;
					if (discriminator?.Mapping?.FirstOrDefault(kvp => kvp.Value.OriginalString == id) is { Key: var mapped })
					{
						id = mapped;
					}
					return new TypeUnionEntry(
						TypeName: inlineSchemas.ToInlineDataType(e).Text,
						Identifier: CSharpNaming.ToPropertyName(id, options.ReservedIdentifiers("object", className)),
						DiscriminatorValue: discriminator == null ? null : id
					);
				}).ToArray()
		);
	}

}
