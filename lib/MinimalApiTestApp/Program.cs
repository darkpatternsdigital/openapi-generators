using DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapTestApis();
app.Run();

namespace DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp
{
	public static class TestApis
	{
		// Each handler interface is resolved per request, so tests register their own implementations
		public static void MapTestApis(this IEndpointRouteBuilder endpoints)
		{
			Headers.InfoApiEndpoints.MapInfoApi(endpoints);
			Headers.RedirectApiEndpoints.MapRedirectApi(endpoints);
			HttpMethods.ItemsIdApiEndpoints.MapItemsIdApi(endpoints);
			HttpTrace.ItemsApiEndpoints.MapItemsApi(endpoints);
			OneOf.DeclaredPetApiEndpoints.MapDeclaredPetApi(endpoints);
			OneOf.IdOrNameApiEndpoints.MapIdOrNameApi(endpoints);
			OneOf.PetApiEndpoints.MapPetApi(endpoints);
			PathLevelParameters.WidgetsWidgetIdApiEndpoints.MapWidgetsWidgetIdApi(endpoints);
			Responses.DefaultApiEndpoints.MapDefaultApi(endpoints);
			Responses.HeadersApiEndpoints.MapHeadersApi(endpoints);
			Responses.MediaTypesApiEndpoints.MapMediaTypesApi(endpoints);
			Responses.RangesApiEndpoints.MapRangesApi(endpoints);
			Responses.RefApiEndpoints.MapRefApi(endpoints);
		}
	}
}

public partial class Program { }
