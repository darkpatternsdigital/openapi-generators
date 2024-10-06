using System;
using DarkPatterns.OpenApiCodegen;

namespace DarkPatterns.OpenApi.Transformations.Diagnostics;


public delegate string? PathResolver(Uri uri);

public static class DiagnosticsConversion
{
	public static Func<DiagnosticBase, DiagnosticInfo> GetConverter(PathResolver resolver) => (DiagnosticBase diagnostic) =>
		new DiagnosticInfo(
			Id: diagnostic.GetType().FullName,
			Location: ToDiagnosticLocation(diagnostic.Location, resolver),
			Metadata: diagnostic.GetTextArguments()
		);

	private static DiagnosticLocation ToDiagnosticLocation(Location location, PathResolver resolver)
	{
		return new DiagnosticLocation(
			location.RetrievalUri switch
			{
				var uri when resolver(uri) is string s => s,
				{ Scheme: "file" } => location.RetrievalUri.LocalPath,
				{ OriginalString: var s } => s
			},
			location.Range == null ? null : new DiagnosticLocationRange(ToDiagnosticLocationMark(location.Range.Start), ToDiagnosticLocationMark(location.Range.End))
		);
	}

	private static DiagnosticLocationMark ToDiagnosticLocationMark(FileLocationMark mark) =>
		new DiagnosticLocationMark(mark.Line, mark.Column);
}
