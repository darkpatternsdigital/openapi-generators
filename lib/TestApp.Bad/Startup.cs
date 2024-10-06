namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddAuthentication(defaultScheme: "Cookies")
			.AddCookie("Cookies");
		services.AddAuthorizationBuilder();

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
