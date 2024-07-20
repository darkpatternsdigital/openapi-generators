namespace DarkPatterns.OpenApi.TypeScript.Templates
{
	public record PartialHeader(
		string? AppName,
		string? AppDescription,
		string? Version,
		string? InfoEmail,
		string CodeGeneratorVersionInfo
	);
}