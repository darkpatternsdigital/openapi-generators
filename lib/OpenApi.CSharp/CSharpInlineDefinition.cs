namespace DarkPatterns.OpenApi.CSharp;

public record CSharpInlineDefinition(string Text, bool Nullable = false, bool IsEnumerable = false)
{
	// Assumes C#8, since it's standard in VS2019+, which is when nullable reference types were introduced
	public CSharpInlineDefinition MakeNullable() =>
		Nullable ? this : new(Text + "?", Nullable: true, IsEnumerable: IsEnumerable);
}
