using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static System.Linq.Expressions.Expression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DarkPatterns.OpenApiCodegen;

using ReportDiagnostic = Action<Diagnostic>;
using AddSourceText = Action<string, SourceText>;

public abstract class BaseGenerator : IIncrementalGenerator
{
	private const string additionalTextInfoAssemblyName = "DarkPatterns.OpenApiCodegen";
	private const string additionalTextInfoTypeName = "DarkPatterns.OpenApiCodegen.AdditionalTextInfo";
	private static readonly DiagnosticDescriptor OpenApiConversionError = new DiagnosticDescriptor(id: "DPD_PARSE_UNK",
																								title: "A conversion error was encountered",
																								messageFormat: "A conversion error was encountered: {0}",
																								category: "DarkPatterns.OpenApiCodegen",
																								DiagnosticSeverity.Error,
																								isEnabledByDefault: true);
	// Roslyn's CS8785 only names the exception type, which hides the cause of exceptions such as
	// TypeInitializationException, so report the whole exception instead.
	private static readonly DiagnosticDescriptor GenerationFailed = new DiagnosticDescriptor(id: "DPDAPI003",
																							title: "Code generation failed",
																							messageFormat: "Code generation failed: {0}",
																							category: "DarkPatterns.OpenApiCodegen",
																							DiagnosticSeverity.Warning,
																							isEnabledByDefault: true);

	private static readonly object lockHandle = new object();

	private readonly Lazy<LoadedGenerator> loaded;

	protected BaseGenerator(string generatorTypeName, string assemblyName)
	{
		// Roslyn reports an exception from this constructor without its details, so load the
		// generator when it's first used, where ReportFailures reports any exception.
		loaded = new(() => Load(generatorTypeName, assemblyName));
	}

	private sealed record LoadedGenerator(
		Func<IEnumerable<string>> GetMetadataKeys,
		Func<string, string, IReadOnlyList<string>, IReadOnlyDictionary<string, string?>, object> ToAdditionalTextType,
		Func<object[], object> Generate);

	private LoadedGenerator Load(string generatorTypeName, string assemblyName)
	{
		var myAsm = this.GetType().Assembly;

		var references = myAsm.GetReferencedAssemblies();

		List<Assembly> loadedAssemblies = new() { myAsm };
		Dictionary<string, Assembly> loadedFromStreamAssemblies = new() { };
		AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly!;
		AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly!;

		// When using Type.GetType, the `RequestingAssembly` ends up null. If a generic is passed to Assembly.GetType, it also comes through as null.
		// See https://github.com/dotnet/runtime/issues/11895, https://github.com/dotnet/runtime/issues/12668
		var generatorType = GetEmbeddedAssemblyByName(assemblyName)?.GetType(generatorTypeName, throwOnError: false)
			?? throw new InvalidOperationException($"Could not find generator {generatorTypeName}");
		var additionalTextType = GetEmbeddedAssemblyByName(additionalTextInfoAssemblyName)?.GetType(additionalTextInfoTypeName, throwOnError: false)
			?? throw new InvalidOperationException($"Could not find type {additionalTextInfoTypeName} in {additionalTextInfoAssemblyName}");

		var generator = Activator.CreateInstance(generatorType);
		var generatorExpression = Constant(generator);

		var toAdditionalTextTypeMethod = generatorType.GetMethod("ToFileInfo")!;
		var pathParameter = Parameter(typeof(string));
		var textParameter = Parameter(typeof(string));
		var typesParamter = Parameter(typeof(IReadOnlyList<string>));
		var dictionaryParameter = Parameter(typeof(IReadOnlyDictionary<string, string?>));
		var getMetadataKeys = Lambda<Func<IEnumerable<string>>>(Property(generatorExpression, "MetadataKeys")).Compile();
		var toAdditionalTextType = Lambda<Func<string, string, IReadOnlyList<string>, IReadOnlyDictionary<string, string?>, object>>(
			Convert(Call(generatorExpression, toAdditionalTextTypeMethod, pathParameter, textParameter, typesParamter, dictionaryParameter), typeof(object))
			, pathParameter, textParameter, typesParamter, dictionaryParameter).Compile();

		var generateMethod = generatorType.GetMethods().FirstOrDefault(m => m.Name == "Generate" && m.GetParameters().Length == 1)!;
		var ofTypeMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.OfType))!.MakeGenericMethod(generateMethod.GetParameters()[0].ParameterType.GetGenericArguments())!;
		var otherFilesParameter = Parameter(typeof(object[]));
		var generate = Lambda<Func<object[], object>>(
			Convert(Call(generatorExpression, generateMethod,
				Call(null, ofTypeMethod, otherFilesParameter)), typeof(object))
			, otherFilesParameter).Compile();

		return new(getMetadataKeys, toAdditionalTextType, generate);

		Assembly? ResolveAssembly(object sender, ResolveEventArgs ev)
		{
			// I'm not sure why this lock makes a difference; maybe by preventing multiple loads of the same assembly.
			// As a result, this maybe can be moved.
			lock (lockHandle)
			{
				if (ev.RequestingAssembly == null)
					// Someone loaded something through Type.GetType or a generic in Assembly.GetType.
					// This project shouldn't do that, so we can safely ignore it.
					return null;
				if (!loadedAssemblies.Contains(ev.RequestingAssembly))
					// If it wasn't one of our assemblies requesting the DLL, do not respond, let something else handle it
					return null;
				if (references.Any(asm => asm.FullName == ev.Name) && AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName == ev.Name) is Assembly currentDomainAsm)
					// If it's something this assembly references (which is only core Roslyn files), return it from the app domain.
					return currentDomainAsm;
				return GetEmbeddedAssemblyByName(ev.Name);
			}
		}

		Assembly? GetEmbeddedAssemblyByName(string name)
		{
			if (loadedAssemblies.FirstOrDefault(asm => asm.FullName == name) is Assembly preloaded)
				return preloaded;

			var namePart = name.Split(',')[0];
			var nameRegex = new Regex("^" + Regex.Escape(namePart) + @"(\.\d+){3}\.dll");
			var streamName = myAsm.GetManifestResourceNames().SingleOrDefault(n => n == namePart + ".dll" || nameRegex.IsMatch(n));
			if (streamName == null) return null;
			if (loadedFromStreamAssemblies.TryGetValue(streamName, out var versionMismatchAssembly))
				return versionMismatchAssembly;

			using var stream = myAsm.GetManifestResourceStream(streamName);
			if (stream != null)
			{
				using var dllBytes = new MemoryStream();
				stream.CopyTo(dllBytes);
#pragma warning disable RS1035 // Assembly.Load is banned for analyzers: the generator embeds its dependencies so it can load them without the compiler resolving them
				var resultAsm = Assembly.Load(dllBytes.ToArray());
#pragma warning restore RS1035
				loadedAssemblies.Add(resultAsm);
				loadedFromStreamAssemblies[streamName] = resultAsm;
				return resultAsm;
			}
			return null;
		}
	}

	record IncrementalData(AdditionalTextWithOptions Entrypoint, IEnumerable<AdditionalTextWithOptions> OtherKnownSchemas);

	public virtual void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterImplementationSourceOutput(context.CompilationProvider, (context, compilation) =>
		{
			ReportFailures(context, () => ReportCompilationDiagnostics(compilation, context));
		});

		// Build an incremental "watcher"
		var allAdditionalTexts = context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (tuple, cancellation) => GetOptions(tuple.Left, tuple.Right))
			.Where(static (tuple) => tuple.TextContents != null)
			.Where((tuple) => GetFileTypes(tuple) is not []);
		context.RegisterSourceOutput(allAdditionalTexts.Collect(), (context, tuple) =>
		{
			ReportFailures(context, () => GenerateSources(tuple, context));
		});
	}

	protected record AdditionalTextWithOptions(string Path, string TextContents, AnalyzerConfigOptions ConfigOptions);
	protected record CompilerApis(AddSourceText AddSource, ReportDiagnostic ReportDiagnostic)
	{
#pragma warning disable CA2225 // Operator overloads have named alternates
		public static implicit operator CompilerApis(SourceProductionContext context) =>
			new(context.AddSource, context.ReportDiagnostic);
#pragma warning restore CA2225 // Operator overloads have named alternates
	}
	private static AdditionalTextWithOptions GetOptions(AdditionalText file, AnalyzerConfigOptionsProvider analyzerConfigOptions)
	{
		var opt = analyzerConfigOptions.GetOptions(file);
		return new(file.Path, file.GetText()?.ToString()!, opt);
	}

	private static void ReportFailures(CompilerApis apis, Action action)
	{
		try
		{
			action();
		}
#pragma warning disable CA1031 // Do not catch general exception types: any exception is reported instead of CS8785
		catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
		{
			// ToString includes inner exceptions and stack traces
			apis.ReportDiagnostic(Diagnostic.Create(GenerationFailed, Location.None, ex.ToString()));
		}
	}

	protected abstract void ReportCompilationDiagnostics(Compilation compilation, CompilerApis apis);
	protected abstract string[] GetFileTypes(AdditionalTextWithOptions additionalText);
	private void GenerateSources(IEnumerable<AdditionalTextWithOptions> additionalText, CompilerApis apis)
	{
		IEnumerable<string> metadataKeys = loaded.Value.GetMetadataKeys();
		var additionalFileInputs = additionalText.Select((f) => AdditionalFileTypeParams.Load(f, metadataKeys, GetFileTypes(f))).ToArray();

		// result is of type DarkPatterns.OpenApiCodegen.GenerationResult
		dynamic result = loaded.Value.Generate(
			additionalFileInputs.Select(ToAdditionalFileType).ToArray()
		);
		foreach (var entry in result.Sources)
		{
			apis.AddSource($"DPD_{entry.Key}", SourceText.From(entry.SourceText, Encoding.UTF8));
		}
		foreach (var diagnostic in result.Diagnostics)
		{
			if (TransformationDiagnostics.DiagnosticBy.TryGetValue((string)diagnostic.Id, out var descriptor))
				apis.ReportDiagnostic(
					Diagnostic.Create(
						descriptor,
						Location.Create(
							diagnostic.Location.FilePath,
							default(TextSpan),
							diagnostic.Location.Range == null ? default : new LinePositionSpan(
								new LinePosition(diagnostic.Location.Range.Start.Line - 1, diagnostic.Location.Range.Start.Column - 1),
								new LinePosition(diagnostic.Location.Range.End.Line - 1, diagnostic.Location.Range.End.Column - 1)
							)
						),
						((IReadOnlyList<object>)diagnostic.Metadata).ToArray()
					)
				);
			else
				apis.ReportDiagnostic(
					Diagnostic.Create(
						OpenApiConversionError,
						Location.Create(
							diagnostic.Location.FilePath,
							default(TextSpan),
							diagnostic.Location.Range == null ? default : new LinePositionSpan(
								new LinePosition(diagnostic.Location.Range.Start.Line - 1, diagnostic.Location.Range.Start.Column - 1),
								new LinePosition(diagnostic.Location.Range.End.Line - 1, diagnostic.Location.Range.End.Column - 1)
							)
						),
						(string)diagnostic.Id
					)
				);
		}
	}

	record AdditionalFileTypeParams(string Path, string TextContents, IReadOnlyList<string> FileTypes, ReadOnlyDictionary<string, string?> Metadata)
	{
		public static AdditionalFileTypeParams Load(AdditionalTextWithOptions additionalText, IEnumerable<string> metadataKeys, IEnumerable<string> fileTypes)
		{
			return new AdditionalFileTypeParams(
					additionalText.Path,
					additionalText.TextContents,
					fileTypes.ToArray(),
					new ReadOnlyDictionary<string, string?>(
						metadataKeys.ToDictionary(key => key, key => GetMetadata(additionalText.ConfigOptions, key))
					)
				);
		}

		// Keys such as `build_property.projectdir` are global (CompilerVisibleProperty) rather than per-file metadata
		private static string? GetMetadata(AnalyzerConfigOptions options, string key) =>
			key.StartsWith("build_property.", StringComparison.Ordinal)
				? (options.TryGetValue(key, out var value) ? value : null)
				: options.GetAdditionalFilesMetadata(key);
	}

	object ToAdditionalFileType(AdditionalFileTypeParams p)
	{
		return loaded.Value.ToAdditionalTextType(
					p.Path,
					p.TextContents,
					p.FileTypes,
					p.Metadata
				);
	}


}
