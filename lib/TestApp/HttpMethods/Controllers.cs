namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.HttpMethods;

public class ItemsController : ItemsIdControllerBase
{
	protected override Task<GetItemActionResult> GetItem(string id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<GetItemActionResult>();
	}

	protected override Task<PutItemActionResult> PutItem(string id, string putItemBody)
	{
		this.DelegateRequest((id, putItemBody));
		return this.DelegateResponse<PutItemActionResult>();
	}

	protected override Task<PostItemActionResult> PostItem(string id, string postItemBody)
	{
		this.DelegateRequest((id, postItemBody));
		return this.DelegateResponse<PostItemActionResult>();
	}

	protected override Task<DeleteItemActionResult> DeleteItem(string id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<DeleteItemActionResult>();
	}

	protected override Task<PatchItemActionResult> PatchItem(string id, string patchItemBody)
	{
		this.DelegateRequest((id, patchItemBody));
		return this.DelegateResponse<PatchItemActionResult>();
	}

	protected override Task<HeadItemActionResult> HeadItem(string id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<HeadItemActionResult>();
	}

	protected override Task<OptionsItemActionResult> OptionsItem(string id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<OptionsItemActionResult>();
	}
}
