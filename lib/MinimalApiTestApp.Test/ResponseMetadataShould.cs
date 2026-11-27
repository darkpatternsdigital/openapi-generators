using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

/// <summary>Endpoints describe their responses for OpenAPI documents and the API explorer</summary>
public class ResponseMetadataShould
{
	private static ApiDescription Describe(string path)
	{
		using var factory = new WebApplicationFactory<Program>();
		var provider = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
		return provider.ApiDescriptionGroups.Items.SelectMany(g => g.Items).Single(d => "/" + d.RelativePath == path);
	}

	[Fact]
	public void Describe_status_code_responses()
	{
		var description = Describe("/responses/ref");

		Assert.Contains(description.SupportedResponseTypes, r => r.StatusCode == 200 && r.Type == typeof(string));
		Assert.Contains(description.SupportedResponseTypes, r => r.StatusCode == 404 && r.Type == typeof(Responses.Error));
	}

	[Fact]
	public void Describe_the_default_response()
	{
		var description = Describe("/responses/default");

		Assert.Contains(description.SupportedResponseTypes, r => r.IsDefaultResponse && r.Type == typeof(Responses.Error));
	}
}
