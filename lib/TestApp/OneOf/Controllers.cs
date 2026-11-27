namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.OneOf;

public class PetController : PetControllerBase
{
	protected override Task<AddPetActionResult> AddPet(SpecifiedPet addPetBody)
	{
		this.DelegateRequest(addPetBody);
		return this.DelegateResponse<AddPetActionResult>();
	}

	protected override Task<GetRandomPetActionResult> GetRandomPet()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetRandomPetActionResult>();
	}
}

public class IdOrNameController : IdOrNameControllerBase
{
	protected override Task<GetIdOrNameActionResult> GetIdOrName()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetIdOrNameActionResult>();
	}
}

public class DeclaredPetController : DeclaredPetControllerBase
{
	protected override Task<AddDeclaredPetActionResult> AddDeclaredPet(DeclaredPet addDeclaredPetBody)
	{
		this.DelegateRequest(addDeclaredPetBody);
		return this.DelegateResponse<AddDeclaredPetActionResult>();
	}
}
