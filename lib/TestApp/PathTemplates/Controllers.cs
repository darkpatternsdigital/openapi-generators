namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.PathTemplates;

public class OwnerPetsController : OwnersOwnerIdPetsPetIdControllerBase
{
	protected override Task<GetOwnerPetActionResult> GetOwnerPet(int ownerId, string petId)
	{
		this.DelegateRequest((ownerId, petId));
		return this.DelegateResponse<GetOwnerPetActionResult>();
	}
}

public class FilesController : FilesNameExtControllerBase
{
	protected override Task<GetFileActionResult> GetFile(string name, string ext)
	{
		this.DelegateRequest((name, ext));
		return this.DelegateResponse<GetFileActionResult>();
	}
}

public class SharedParametersController : SharedParametersControllerBase
{
	protected override Task<GetWithSharedParametersActionResult> GetWithSharedParameters(int limit, string? xTraceId)
	{
		this.DelegateRequest((limit, xTraceId));
		return this.DelegateResponse<GetWithSharedParametersActionResult>();
	}
}

public class NoOperationIdController : NoOperationIdIdControllerBase
{
	protected override Task<GetPathsNoOperationIdIdActionResult> GetPathsNoOperationIdId(string id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<GetPathsNoOperationIdIdActionResult>();
	}
}
