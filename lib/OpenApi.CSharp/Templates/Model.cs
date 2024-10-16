﻿using DarkPatterns.OpenApiCodegen.Handlebars.Templates;

namespace DarkPatterns.OpenApi.CSharp.Templates;

public record ModelTemplate<TModel>(
	PartialHeader Header,

	string PackageName,
	string SourceSchemaId,

	TModel Model
) where TModel : Model;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
public record Model(
	string? Description,
	string ClassName
);

public record EnumModel(
	string? Description,
	string ClassName,
	bool IsString,
	EnumVar[] EnumVars
) : Model(Description, ClassName);

public record TypeUnionModel(
	string? Description,
	string ClassName,
	bool AllowAnyOf,
	string? DiscriminatorProperty,
	TypeUnionEntry[] TypeEntries
) : Model(Description, ClassName);

public record TypeUnionEntry(
	string TypeName,
	string Identifier,
	string? DiscriminatorValue
);

public record EnumVar(
	string Name,
	string Value
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
public record ObjectModel(
	string? Description,
	string ClassName,
	string? Parent,
	ModelVar[] Vars
) : Model(Description, ClassName);

public record ModelVar(
	string BaseName,
	string DataType,
	bool Nullable,
	bool IsContainer,
	string Name,
	bool Required,
	bool Optional,
	string? Pattern,
	int? MinLength,
	int? MaxLength,
	decimal? Minimum,
	decimal? Maximum
)
{
	public bool HasMinLength => MinLength.HasValue;
	public bool HasMaxLength => MaxLength.HasValue;

	public bool HasMinimum => Minimum.HasValue;
	public bool HasMaximum => Maximum.HasValue;
}
