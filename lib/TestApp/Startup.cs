namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddAuthentication(defaultScheme: "Cookies")
			.AddCookie("Cookies")
			.AddOauthYamlPolicies()
			.AddServerSecurityPolicies();
		services.AddAuthorizationBuilder()
			.AddOauthYamlPolicies()
			.AddServerSecurityPolicies();

		services.AddControllers();
	}
	public void Configure(IApplicationBuilder app)
	{
		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
		});
	}
}
