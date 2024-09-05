using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkPatterns.OpenApi.CSharp
{
	public class CSharpSchemaOptions
	{
		public CSharpSchemaExtensionsOptions Extensions { get; set; } = new();
		public List<string> GlobalReservedIdentifiers { get; } = new();
		public Dictionary<string, List<string>> ContextualReservedIdentifiers { get; } = new();
		public string MapType { get; set; } = "global::System.Collections.Generic.Dictionary<string, {}>";
		public string ArrayType { get; set; } = "global::System.Collections.Generic.IEnumerable<{}>";
		public string FallbackType { get; set; } = "object";
		public string DefaultNamespace { get; set; } = "";
		public Dictionary<string, string> OverrideNames { get; set; } = new();
		public Dictionary<Uri, string> NamespacesBySchema { get; set; } = new();
		public Dictionary<string, OpenApiTypeFormats> Types { get; } = new();

		internal string Find(string type, string? format)
		{
			if (!Types.TryGetValue(type, out var formats))
				return FallbackType;
			if (format == null || !formats.Formats.TryGetValue(format, out var result))
				return formats.Default;
			return result;
		}

		internal string ToArrayType(string type)
		{
			return ArrayType.Replace("{}", type);
		}

		internal string ToMapType(string type)
		{
			return MapType.Replace("{}", type);
		}

		public static System.IO.Stream GetDefaultOptionsJson() =>
			typeof(CSharpSchemaOptions).Assembly.GetManifestResourceStream($"{typeof(CSharpSchemaOptions).Namespace}.csharp.config.yaml");

		public IEnumerable<string> ReservedIdentifiers(string? scope = null, params string[] extraReserved) =>
			(
				scope is not null && ContextualReservedIdentifiers.TryGetValue(scope, out var scopedContextualIdentifiers)
					? GlobalReservedIdentifiers.Concat(scopedContextualIdentifiers)
					: GlobalReservedIdentifiers
			).Concat(
				extraReserved
			);

		internal string GetNamespace(Uri schemaId)
		{
			if (OverrideNames.TryGetValue(schemaId.OriginalString, out var fullName)) return fullName.Substring(0, fullName.LastIndexOf('.'));
			if (NamespacesBySchema.TryGetValue(schemaId, out var result)) return result;
			return DefaultNamespace;
		}

		internal string ToClassName(Uri schemaId, string nameFromFragment)
		{
			if (OverrideNames.TryGetValue(schemaId.OriginalString, out var fullName)) return fullName.Substring(fullName.LastIndexOf('.') + 1);
			return CSharpNaming.ToClassName(nameFromFragment, ReservedIdentifiers());
		}
	}

	public class OpenApiTypeFormats
	{
		public string Default { get; set; } = "object";
		public Dictionary<string, string> Formats { get; } = new();
	}
}