using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;

public class OneOfYamlShould
{
	[Fact]
	public Task Handle_OneOf_without_discriminator() =>
		TestSingleRequest<OneOf.PetControllerBase.GetRandomPetActionResult>(new(
			OneOf.PetControllerBase.GetRandomPetActionResult.Ok(new(Dog: new(true, "Shiba Inu"))),
			client => client.GetAsync("/one-of/pet")
		)
		{
			AssertResponseMessage = VerifyResponse(200, new { bark = true, breed = "Shiba Inu" })
		});

	[Fact]
	public Task Handle_OneOf_with_discriminator() =>
		TestSingleRequest<OneOf.PetControllerBase.AddPetActionResult, OneOf.SpecifiedPet>(new(
			OneOf.PetControllerBase.AddPetActionResult.Ok(),
			client => client.PostAsync("/one-of/pet", JsonContent.Create(new { petType = "dog", bark = true, breed = "Shiba Inu" }))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				var dog = Assert.IsType<OneOf.Dog>(request?.Dog);
				Assert.Equal("Shiba Inu", dog?.Breed);
				Assert.Equal(true, dog?.Bark);
			},
			AssertResponseMessage = VerifyResponse(200)
		});

	[Fact]
	public Task Handle_invalid_OneOf_with_discriminator() =>
		TestValidationError(
			OneOf.PetControllerBase.AddPetActionResult.Unsafe,
			client => client.PostAsync("/one-of/pet", JsonContent.Create(new { petType = "Dog" }))
		);

	[Fact]
	public Task Handle_OneOf_with_discriminator_declared_by_each_case() =>
		TestSingleRequest<OneOf.DeclaredPetControllerBase.AddDeclaredPetActionResult, OneOf.DeclaredPet>(new(
			OneOf.DeclaredPetControllerBase.AddDeclaredPetActionResult.Ok(),
			client => client.PostAsync("/one-of/declared-pet", JsonContent.Create(new { petType = "dog", bark = true }))
		)
		{
			AssertRequest = (controller, request) =>
			{
				Assert.True(controller.ModelState.IsValid);
				var dog = Assert.IsType<OneOf.DeclaredDog>(request?.Dog);
				Assert.True(dog.Bark);
			},
			AssertResponseMessage = VerifyResponse(200)
		});

	[Fact]
	public Task Report_the_location_of_errors_inside_a_OneOf_case() =>
		TestSingleRequest<OneOf.DeclaredPetControllerBase.AddDeclaredPetActionResult, OneOf.DeclaredPet>(new(
			OneOf.DeclaredPetControllerBase.AddDeclaredPetActionResult.Unsafe(new BadRequestResult(), []),
			client => client.PostAsync("/one-of/declared-pet", JsonContent.Create(new { petType = "dog", bark = "yes" }))
		)
		{
			AssertRequest = (controller, _) =>
			{
				Assert.False(controller.ModelState.IsValid);
#if NET11_0_OR_GREATER
				// On .NET 11, oneOf models are union types, so System.Text.Json reads the case and reports the property inside it
				Assert.Contains("$.bark", controller.ModelState.Keys);
				Assert.DoesNotContain("$", controller.ModelState.Keys);
#else
				// The JsonConverter can only report the location of the oneOf value (the body, `$`); its message names the property
				Assert.DoesNotContain("$.bark", controller.ModelState.Keys);
				var errors = controller.ModelState["$"]?.Errors ?? throw new Xunit.Sdk.XunitException("No ModelState entry for the oneOf value");
				Assert.Contains(errors, e => e.Exception?.Message.Contains("$.bark", StringComparison.Ordinal) == true || e.ErrorMessage.Contains("$.bark", StringComparison.Ordinal));
#endif
			},
			AssertResponseMessage = VerifyResponse(400)
		});
}
