using System.Collections.Generic;

namespace DarkPatterns.Json.Documents;

public record DocumentRegistryOptions(
	IReadOnlyList<DocumentResolver> Resolvers
);
