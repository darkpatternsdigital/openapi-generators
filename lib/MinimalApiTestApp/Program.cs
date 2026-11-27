using DarkPatterns.OpenApiCodegen.Server.MinimalApi.TestApp;

var builder = WebApplication.CreateBuilder(args);
// So tests can check the response metadata that OpenAPI documents are built from
builder.Services.AddEndpointsApiExplorer();
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
			Enum.DifficultEnumApiEndpoints.MapDifficultEnumApi(endpoints);
			Enum.RockPaperScissorsApiEndpoints.MapRockPaperScissorsApi(endpoints);
			Enum.RockPaperScissorsQueryApiEndpoints.MapRockPaperScissorsQueryApi(endpoints);
			// Form endpoints need antiforgery, which the generated code leaves to the app
			Form.BasicApiEndpoints.MapBasicApi(endpoints).DisableAntiforgery();
			ValidationOptional.FormApiEndpoints.MapFormApi(endpoints).DisableAntiforgery();
			ValidationOptional.JsonApiEndpoints.MapJsonApi(endpoints);
			CookieParameters.SessionApiEndpoints.MapSessionApi(endpoints);
			RequestBodies.BinaryApiEndpoints.MapBinaryApi(endpoints);
			RequestBodies.MultipleApiEndpoints.MapMultipleApi(endpoints).DisableAntiforgery();
			RequestBodies.OptionalApiEndpoints.MapOptionalApi(endpoints);
			RequestBodies.RefApiEndpoints.MapRefApi(endpoints);
			RequestBodies.TextApiEndpoints.MapTextApi(endpoints);
			FormAndCookieTypes.FormTypesApiEndpoints.MapFormTypesApi(endpoints).DisableAntiforgery();
			FormAndCookieTypes.CookieTypesApiEndpoints.MapCookieTypesApi(endpoints);
			FormAndCookieTypes.QueryAndHeaderTypesApiEndpoints.MapQueryAndHeaderTypesApi(endpoints);
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
