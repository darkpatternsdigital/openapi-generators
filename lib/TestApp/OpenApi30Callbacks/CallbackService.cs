namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.OpenApi30Callbacks;

public class CallbackService(HttpClient client)
{
	public async Task<WebhookOperations.PostOnDataReturnType> Invoke()
	{
		return await client.PostOnData(new Uri("https://example.com/foo"));
	}
}