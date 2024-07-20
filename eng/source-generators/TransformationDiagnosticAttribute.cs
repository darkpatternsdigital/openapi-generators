using System;

namespace DarkPatterns.OpenApiCodegen;

[AttributeUsage(AttributeTargets.Field)]
public class TransformationDiagnosticAttribute(string fullTypeName) : Attribute
{
	public string FullTypeName => fullTypeName;
}