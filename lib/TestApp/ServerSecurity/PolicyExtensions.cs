using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Test authentication for oas-server-security.yml: one authentication scheme and one policy per security scheme,
/// named as in the document. Each policy requires a user authenticated by its own scheme, so a request that must
/// satisfy several requirements at once only passes when each scheme authenticated it.
/// </summary>
public static class ServerSecurityPolicyExtensions
{
	public const string SchemeClaimType = "oas-security-scheme";
	public const string ScopeClaimType = "scope";

	private static readonly Dictionary<string, Func<HttpRequest, string?>> credentialReaders = new()
	{
		["HeaderKey"] = request => request.Headers["X-Header-Key"].FirstOrDefault(),
		["QueryKey"] = request => request.Query["api_key"].FirstOrDefault(),
		["CookieKey"] = request => request.Cookies["api_key"],
		["Basic"] = request => ReadAuthorization(request, "Basic") is { } encoded
			? Encoding.UTF8.GetString(Convert.FromBase64String(encoded))
			: null,
		["Bearer"] = request => ReadAuthorization(request, "Bearer"),
		["Oidc"] = request => ReadAuthorization(request, "Bearer"),
		["OAuth"] = request => ReadAuthorization(request, "Bearer"),
	};

	private static string? ReadAuthorization(HttpRequest request, string scheme)
	{
		var header = request.Headers.Authorization.FirstOrDefault();
		return header != null && header.StartsWith(scheme + " ", StringComparison.OrdinalIgnoreCase)
			? header[(scheme.Length + 1)..]
			: null;
	}

	public static AuthenticationBuilder AddServerSecurityPolicies(this AuthenticationBuilder builder)
	{
		foreach (var (name, readCredential) in credentialReaders)
			builder.AddScheme<ServerSecurityOptions, ServerSecurityHandler>(name, options => options.ReadCredential = readCredential);
		return builder;
	}

	public static AuthorizationBuilder AddServerSecurityPolicies(this AuthorizationBuilder builder)
	{
		foreach (var name in credentialReaders.Keys)
			builder.AddPolicy(name, policy =>
			{
				policy.AddAuthenticationSchemes(name);
				policy.RequireAuthenticatedUser();
				policy.RequireClaim(SchemeClaimType, name);
			});
		return builder;
	}

	public class ServerSecurityOptions : AuthenticationSchemeOptions
	{
		public Func<HttpRequest, string?> ReadCredential { get; set; } = _ => null;
	}

	public class ServerSecurityHandler(IOptionsMonitor<ServerSecurityOptions> options, ILoggerFactory logger, UrlEncoder encoder)
		: AuthenticationHandler<ServerSecurityOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var credential = Options.ReadCredential(Request);
			if (string.IsNullOrEmpty(credential))
				return Task.FromResult(AuthenticateResult.NoResult());

			var identity = new ClaimsIdentity(Scheme.Name);
			identity.AddClaim(new Claim(ClaimTypes.Name, credential));
			identity.AddClaim(new Claim(SchemeClaimType, Scheme.Name));
			// Tokens are written as "scopes:read,write" to grant scopes
			if (credential.StartsWith("scopes:", StringComparison.Ordinal))
			{
				foreach (var scope in credential["scopes:".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries))
					identity.AddClaim(new Claim(ScopeClaimType, scope));
			}
			var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}
