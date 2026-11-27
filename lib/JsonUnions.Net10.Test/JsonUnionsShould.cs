using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DarkPatterns.JsonUnions.Net10.Test.OneOf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DarkPatterns.JsonUnions.Net10.Test;

/// <summary>
/// On .NET 10, an options file sets jsonUnions, and the project references System.Text.Json 11 and PolySharp
/// (for UnionAttribute and IUnion), so oneOf models are union types as on .NET 11.
/// </summary>
public class JsonUnionsShould
{
	private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

	[Fact]
	public void Run_with_System_Text_Json_11()
	{
		Assert.Equal(11, typeof(JsonSerializer).Assembly.GetName().Version?.Major);
	}

	[Theory]
	[InlineData(typeof(Pet))]
	[InlineData(typeof(IdOrName))]
	[InlineData(typeof(DeclaredPet))]
	public void Generate_union_types(Type type)
	{
		Assert.Contains(type.GetCustomAttributes(false), a => a.GetType().FullName == "System.Runtime.CompilerServices.UnionAttribute");
		Assert.Contains(type.GetInterfaces(), i => i.FullName == "System.Runtime.CompilerServices.IUnion");
	}

	[Fact]
	public void Deserialize_each_case()
	{
		Assert.Equal(5L, JsonSerializer.Deserialize<IdOrName>("5", Web)?._0);
		Assert.Equal("Fido", JsonSerializer.Deserialize<IdOrName>("\"Fido\"", Web)?._1);
		Assert.Equal(true, JsonSerializer.Deserialize<DeclaredPet>("""{ "petType": "dog", "bark": true }""", Web)?.Dog?.Bark);
		Assert.Equal(false, JsonSerializer.Deserialize<DeclaredPet>("""{ "petType": "cat", "hunts": false }""", Web)?.Cat?.Hunts);
	}

	[Theory]
	[InlineData("""{ "petType": "dog", "bark": true }""")]
	[InlineData("""{ "petType": "cat", "hunts": false }""")]
	public void Round_trip_a_discriminated_case(string json)
	{
		var pet = JsonSerializer.Deserialize<DeclaredPet>(json, Web);

		Assert.Equal(JsonDocument.Parse(json).RootElement.GetRawText().Replace(" ", ""), JsonSerializer.Serialize(pet, Web));
	}

	[Theory]
	// Both cases are valid
	[InlineData(typeof(Pet), """{ "bark": true, "breed": "Shiba Inu", "hunts": false, "age": 12 }""")]
	// Neither case is valid
	[InlineData(typeof(Pet), """{ "bark": true }""")]
	// Not an object
	[InlineData(typeof(DeclaredPet), "\"dog\"")]
	// No discriminator
	[InlineData(typeof(DeclaredPet), """{ "bark": true }""")]
	// An unknown discriminator
	[InlineData(typeof(DeclaredPet), """{ "petType": "fish" }""")]
	public void Throw_JsonException_at_the_oneOf_value_for_invalid_values(Type type, string json)
	{
		var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("[" + json + "]", typeof(List<>).MakeGenericType(type), Web));

		Assert.Equal("$[0]", ex.Path);
	}

	[Fact]
	public void Report_the_full_path_of_errors_inside_a_case()
	{
		var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<DeclaredPet>>("""[{ "petType": "dog", "bark": "yes" }]""", Web));

		Assert.Equal("$[0].bark", ex.Path);
	}

	[Fact]
	public async Task Use_the_full_path_as_the_ModelState_key()
	{
		await using var app = await StartAppAsync();
		using var client = app.GetTestClient();

		var response = await client.PostAsync("/declared-pet", new StringContent("""{ "petType": "dog", "bark": "yes" }""", Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
		Assert.Contains("$.bark", problem!.Errors.Keys);
		Assert.DoesNotContain("$", problem.Errors.Keys);
	}

	private static async Task<WebApplication> StartAppAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddControllers().AddApplicationPart(typeof(DeclaredPetController).Assembly);
		var app = builder.Build();
		app.MapControllers();
		await app.StartAsync();
		return app;
	}
}

// MVC finds only top-level public controllers
public class DeclaredPetController : DeclaredPetControllerBase
{
	protected override Task<AddDeclaredPetActionResult> AddDeclaredPet(DeclaredPet body) =>
		Task.FromResult(ModelState.IsValid
			? AddDeclaredPetActionResult.Ok()
			: AddDeclaredPetActionResult.Unsafe(new BadRequestObjectResult(new ValidationProblemDetails(ModelState)), []));
}
