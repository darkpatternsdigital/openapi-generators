﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DarkPatterns.OpenApiCodegen;

public record SourceEntry(string Key, string SourceText);


[DebuggerDisplay("{Line},{Column}")]
public record DiagnosticLocationMark(int Line, int Column);
[DebuggerDisplay("{Start},{End}")]
public record DiagnosticLocationRange(DiagnosticLocationMark Start, DiagnosticLocationMark End)
{
}

public record DiagnosticLocation(string FilePath, DiagnosticLocationRange? Range);

public record DiagnosticInfo(string Id, DiagnosticLocation Location, IReadOnlyList<string> Metadata);

public record AdditionalTextInfo(string Path, string Contents, IReadOnlyDictionary<string, string?> Metadata);

public record GenerationResult(IReadOnlyList<SourceEntry> Sources, IReadOnlyList<DiagnosticInfo> Diagnostics);

// Note: This interface is not used directly, but is used by the `BaseGenerator` via reflection/compiled lambdas
public interface IOpenApiCodeGenerator
{
	IEnumerable<string> MetadataKeys { get; }

	AdditionalTextInfo ToFileInfo(string documentPath, string documentContents, IReadOnlyDictionary<string, string?> additionalTextMetadata);
	GenerationResult Generate(AdditionalTextInfo entrypoint, IEnumerable<AdditionalTextInfo> other);
}
