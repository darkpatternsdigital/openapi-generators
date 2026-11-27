using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Json.Documents;
using DarkPatterns.Json.Specifications;

namespace DarkPatterns.OpenApi.Abstractions;

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#security-scheme-object
/// </summary>
public record OpenApiSecurityScheme(
	Uri Id,
	SecuritySchemeType Type,
	string? Description,
	// apiKey
	string? Name,
	ApiKeyLocation? In,
	// http
	string? Scheme,
	string? BearerFormat,
	// oauth2
	OpenApiOAuthFlows? Flows,
	// openIdConnect
	Uri? OpenIdConnectUrl
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes()
	{
		if (Flows != null)
			yield return Flows;
	}
}

public enum SecuritySchemeType
{
	ApiKey,
	Http,
	OAuth2,
	OpenIdConnect,
}

public enum ApiKeyLocation
{
	Query,
	Header,
	Cookie,
}

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#oauth-flows-object
/// </summary>
public record OpenApiOAuthFlows(
	Uri Id,
	OpenApiOAuthFlow? Implicit,
	OpenApiOAuthFlow? Password,
	OpenApiOAuthFlow? ClientCredentials,
	OpenApiOAuthFlow? AuthorizationCode
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		new[] { Implicit, Password, ClientCredentials, AuthorizationCode }.OfType<OpenApiOAuthFlow>();
}

/// <summary>
/// See https://spec.openapis.org/oas/v3.1.0#oauth-flow-object
/// </summary>
public record OpenApiOAuthFlow(
	Uri Id,
	Uri? AuthorizationUrl,
	Uri? TokenUrl,
	Uri? RefreshUrl,
	IReadOnlyDictionary<string, string> Scopes
) : IReferenceableDocumentNode
{
	public NodeMetadata Metadata => new NodeMetadata(Id);

	public IEnumerable<IJsonDocumentNode> GetNestedNodes() =>
		Enumerable.Empty<IJsonDocumentNode>();
}
