using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using ServerSecurity;

/// <summary>
/// Uses the authentication schemes and policies in TestApp's ServerSecurity/PolicyExtensions.cs, one per security
/// scheme in oas-server-security.yml.
/// </summary>
public class ServerSecurityYamlShould
{
	private static Func<HttpClient, Task<HttpResponseMessage>> Get(string path, params (string Name, string Value)[] headers) =>
		client =>
		{
			var request = new HttpRequestMessage(HttpMethod.Get, path);
			foreach (var (name, value) in headers)
				request.Headers.TryAddWithoutValidation(name, value);
			return client.SendAsync(request);
		};

	// Like TestRequestBlockedByFramework, but if the request reaches the controller, it gets a response, so the failure shows the status code
	private static async Task TestBlocked<T>(T response, Func<HttpClient, Task<HttpResponseMessage>> performRequest, params int[] statusCodes)
	{
		using var factory = new TestAppFactory();
		factory.OverrideServices += services => services.AddSingleton<IProvideArbitraryResponse<T>>(new ProvideArbitraryResponse<T>(response, _ => { }));
		using var client = factory.CreateDefaultClient();
		using var message = await performRequest(client);
		Assert.Contains((int)message.StatusCode, statusCodes);
	}

	private static string BasicCredentials(string user, string password) =>
		"Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

	[Fact]
	public Task Apply_document_level_security_to_operations_without_their_own() =>
		TestBlocked(DocumentLevelControllerBase.GetDocumentLevelActionResult.NoContent(), Get("/security/document-level"), 401);

	[Fact]
	public Task Allow_requests_that_satisfy_document_level_security() =>
		TestSingleRequest<DocumentLevelControllerBase.GetDocumentLevelActionResult>(new(
			DocumentLevelControllerBase.GetDocumentLevelActionResult.NoContent(),
			Get("/security/document-level", ("X-Header-Key", "key"))
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Allow_anonymous_requests_when_security_is_empty() =>
		TestSingleRequest<PublicControllerBase.GetPublicActionResult>(new(
			PublicControllerBase.GetPublicActionResult.NoContent(),
			Get("/security/public")
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Allow_api_keys_in_the_query() =>
		TestSingleRequest<QueryKeyControllerBase.GetQueryKeyActionResult>(new(
			QueryKeyControllerBase.GetQueryKeyActionResult.NoContent(),
			Get("/security/query-key?api_key=key")
		)
		{
			AssertRequest = (controller, _) => Assert.True(controller.User.Identity?.IsAuthenticated),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_without_the_query_api_key() =>
		TestBlocked(QueryKeyControllerBase.GetQueryKeyActionResult.NoContent(), Get("/security/query-key"), 401);

	[Fact]
	public Task Ignore_scopes_on_api_key_schemes() =>
		TestSingleRequest<ApiKeyScopesControllerBase.GetApiKeyScopesActionResult>(new(
			ApiKeyScopesControllerBase.GetApiKeyScopesActionResult.NoContent(),
			Get("/security/api-key-scopes?api_key=key")
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Allow_api_keys_in_a_cookie() =>
		TestSingleRequest<CookieKeyControllerBase.GetCookieKeyActionResult>(new(
			CookieKeyControllerBase.GetCookieKeyActionResult.NoContent(),
			Get("/security/cookie-key", ("Cookie", "api_key=key"))
		)
		{
			AssertRequest = (controller, _) => Assert.True(controller.User.Identity?.IsAuthenticated),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_without_the_cookie_api_key() =>
		TestBlocked(CookieKeyControllerBase.GetCookieKeyActionResult.NoContent(), Get("/security/cookie-key"), 401);

	[Fact]
	public Task Allow_http_basic_authentication() =>
		TestSingleRequest<BasicControllerBase.GetBasicActionResult>(new(
			BasicControllerBase.GetBasicActionResult.NoContent(),
			Get("/security/basic", ("Authorization", BasicCredentials("user", "pass")))
		)
		{
			AssertRequest = (controller, _) => Assert.Equal("user:pass", controller.User.Identity?.Name),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_without_http_basic_authentication() =>
		TestBlocked(BasicControllerBase.GetBasicActionResult.NoContent(), Get("/security/basic", ("Authorization", "Bearer token")), 401);

	[Fact]
	public Task Allow_http_bearer_authentication() =>
		TestSingleRequest<BearerControllerBase.GetBearerActionResult>(new(
			BearerControllerBase.GetBearerActionResult.NoContent(),
			Get("/security/bearer", ("Authorization", "Bearer token"))
		)
		{
			AssertRequest = (controller, _) => Assert.Equal("token", controller.User.Identity?.Name),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_without_http_bearer_authentication() =>
		TestBlocked(BearerControllerBase.GetBearerActionResult.NoContent(), Get("/security/bearer"), 401);

	[Fact]
	public Task Allow_open_id_connect_authentication() =>
		TestSingleRequest<OpenIdConnectControllerBase.GetOpenIdConnectActionResult>(new(
			OpenIdConnectControllerBase.GetOpenIdConnectActionResult.NoContent(),
			Get("/security/open-id-connect", ("Authorization", "Bearer scopes:read"))
		)
		{
			AssertRequest = (controller, _) => Assert.True(controller.User.Identity?.IsAuthenticated),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Allow_oauth2_tokens_with_the_required_scopes() =>
		TestSingleRequest<OauthScopesControllerBase.GetOAuthScopesActionResult>(new(
			OauthScopesControllerBase.GetOAuthScopesActionResult.NoContent(),
			Get("/security/oauth-scopes", ("Authorization", "Bearer scopes:read,write"))
		)
		{
			AssertRequest = (controller, _) => Assert.True(controller.User.Identity?.IsAuthenticated),
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_oauth2_tokens_without_the_required_scopes() =>
		TestBlocked(OauthScopesControllerBase.GetOAuthScopesActionResult.NoContent(), Get("/security/oauth-scopes", ("Authorization", "Bearer scopes:read")), 403);

	[Fact]
	public Task Allow_requests_that_satisfy_every_scheme_of_a_requirement() =>
		TestSingleRequest<AllOfControllerBase.GetAllOfActionResult>(new(
			AllOfControllerBase.GetAllOfActionResult.NoContent(),
			Get("/security/all-of?api_key=key", ("X-Header-Key", "key"))
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_that_satisfy_only_one_scheme_of_a_requirement() =>
		TestBlocked(AllOfControllerBase.GetAllOfActionResult.NoContent(), Get("/security/all-of", ("X-Header-Key", "key")), 401, 403);

	[Fact]
	public Task Allow_requests_that_satisfy_the_first_of_several_requirements() =>
		TestSingleRequest<AnyOfControllerBase.GetAnyOfActionResult>(new(
			AnyOfControllerBase.GetAnyOfActionResult.NoContent(),
			Get("/security/any-of", ("X-Header-Key", "key"))
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Allow_requests_that_satisfy_the_second_of_several_requirements() =>
		TestSingleRequest<AnyOfControllerBase.GetAnyOfActionResult>(new(
			AnyOfControllerBase.GetAnyOfActionResult.NoContent(),
			Get("/security/any-of?api_key=key")
		)
		{
			AssertResponseMessage = VerifyResponse(204),
		});

	[Fact]
	public Task Block_requests_that_satisfy_none_of_several_requirements() =>
		TestBlocked(AnyOfControllerBase.GetAnyOfActionResult.NoContent(), Get("/security/any-of"), 401, 403);
}
