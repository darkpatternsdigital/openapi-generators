namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Multipart;

public class UploadController : UploadControllerBase
{
	protected override Task<UploadActionResult> Upload(UploadRequest uploadBody)
	{
		this.DelegateRequest<UploadRequest?>(uploadBody);
		return this.DelegateResponse<UploadActionResult>();
	}
}

public class FieldsController : FieldsControllerBase
{
	protected override Task<PostFieldsActionResult> PostFields(PostFieldsRequest postFieldsBody)
	{
		this.DelegateRequest<PostFieldsRequest?>(postFieldsBody);
		return this.DelegateResponse<PostFieldsActionResult>();
	}
}
