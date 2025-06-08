
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.TypeScript;

public static class TypeScriptInlineSchemasExtensions
{
	public static IEnumerable<Templates.ExportFromStatement> GetExportStatements(this TypeScriptInlineSchemas inlineSchemas, IEnumerable<JsonSchema> schemasReferenced, TypeScriptSchemaOptions options, string path)
	{
		// FIXME: this is very hacked together; this accesses the "inline" data type to determine what should be exported
		return from entry in schemasReferenced
			   let t = inlineSchemas.ToInlineDataType(entry)
			   from import in t.Imports
			   from refName in new[] { new Templates.ExportMember(import.Member, IsType: true) }.Concat(GetAdditionalModuleMembers(t, TypeScriptTypeInfo.From(entry), options))
			   let fileName = inlineSchemas.GetFilePath(import)
			   group refName by fileName into imports
			   let nodePath = imports.Key.ToRelativeNodePath(path)
			   orderby nodePath
			   select new Templates.ExportFromStatement(imports.Distinct().OrderBy(a => a.MemberName).ToArray(), nodePath);
	}


	public static IEnumerable<Templates.ImportStatement> GetImportStatements(this TypeScriptInlineSchemas inlineSchemas, IEnumerable<JsonSchema?> schemasReferenced, IEnumerable<JsonSchema?> excludedSchemas, string path)
	{
		return inlineSchemas.ToImportStatements(from entry in schemasReferenced.Except(excludedSchemas)
												let t = inlineSchemas.ToInlineDataType(entry)
												from import in t.Imports
												select import,
												excludedSchemas, path);
	}
	public static IEnumerable<Templates.ImportStatement> ToImportStatements(this TypeScriptInlineSchemas inlineSchemas, IEnumerable<TypeScriptImportReference> importReferences, IEnumerable<JsonSchema?> excludedSchemas, string path)
	{
		var excludedSchemaIds = excludedSchemas.Select(s => s?.Metadata.Id.OriginalString);
		return from import in importReferences
			   where !excludedSchemaIds.Contains(import.Schema.Metadata.Id.OriginalString)
			   let refName = import.Member
			   let fileName = inlineSchemas.GetFilePath(import)
			   group refName by fileName into imports
			   let nodePath = imports.Key.ToRelativeNodePath(path)
			   orderby nodePath
			   select new Templates.ImportStatement(imports.Distinct().OrderBy(a => a).ToArray(), nodePath);
	}


	private static IEnumerable<Templates.ExportMember> GetAdditionalModuleMembers(TypeScriptInlineDefinition t, TypeScriptTypeInfo schema, TypeScriptSchemaOptions options)
	{
		switch (schema)
		{
			case { Enum: { Count: > 0 } }:
				yield return new Templates.ExportMember(
					TypeScriptNaming.ToPropertyName(t.Text, options.ReservedIdentifiers()),
					IsType: false
				);
				break;
		}
	}

	public static string ToRelativeNodePath(this string path, string fromPath)
	{
		if (path.StartsWith("..")) throw new ArgumentException("Cannot start with ..", nameof(path));
		if (fromPath.StartsWith("..")) throw new ArgumentException("Cannot start with ..", nameof(fromPath));
		path = Normalize(path);
		fromPath = Normalize(fromPath);
		var pathParts = path.Split('/');
		pathParts[pathParts.Length - 1] = System.IO.Path.GetFileNameWithoutExtension(pathParts[pathParts.Length - 1]);
		var fromPathParts = System.IO.Path.GetDirectoryName(fromPath).Split('/');
		var ignored = pathParts.TakeWhile((p, i) => i < fromPathParts.Length && p == fromPathParts[i]).Count();
		pathParts = pathParts.Skip(ignored).ToArray();
		fromPathParts = fromPathParts.Skip(ignored).ToArray();
		return string.Join("/", Enumerable.Repeat(".", 1).Concat(Enumerable.Repeat("..", fromPathParts.Length).Concat(pathParts)));

		string Normalize(string p)
		{
			p = p.Replace('\\', '/');
			if (p.StartsWith("./")) p = p.Substring(2);
			return p;
		}
	}
}