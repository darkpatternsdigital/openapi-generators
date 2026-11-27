using DarkPatterns.OpenApiCodegen.Json.Extensions;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.RequestBodies;

public class TextController : TextControllerBase
{
	protected override Task<PostTextActionResult> PostText(string postTextBody)
	{
		this.DelegateRequest(postTextBody);
		return this.DelegateResponse<PostTextActionResult>();
	}
}

public class BinaryController : BinaryControllerBase
{
	protected override async Task<PostBinaryActionResult> PostBinary(Stream postBinaryBody)
	{
		using var buffer = new MemoryStream();
		if (postBinaryBody != null)
			await postBinaryBody.CopyToAsync(buffer);
		this.DelegateRequest(buffer.ToArray());
		return await this.DelegateResponse<PostBinaryActionResult>();
	}
}

public class OptionalController : OptionalControllerBase
{
	protected override Task<PostOptionalActionResult> PostOptional(IOptional<Note> postOptionalBody)
	{
		this.DelegateRequest<IOptional<Note>?>(postOptionalBody);
		return this.DelegateResponse<PostOptionalActionResult>();
	}
}

public class MultipleController : MultipleControllerBase
{
	protected override Task<PostMultipleActionResult> PostMultipleapplicationJson(Note postMultipleBody)
	{
		this.DelegateRequest<(string MediaType, string? Text)>((MediaType: "application/json", Text: postMultipleBody?.Text));
		return this.DelegateResponse<PostMultipleActionResult>();
	}

	protected override Task<PostMultipleActionResult> PostMultipleapplicationXWwwFormUrlencoded(string text)
	{
		this.DelegateRequest<(string MediaType, string? Text)>((MediaType: "application/x-www-form-urlencoded", Text: text));
		return this.DelegateResponse<PostMultipleActionResult>();
	}
}

public class RefController : RefControllerBase
{
	protected override Task<PostRefActionResult> PostRef(Note postRefBody)
	{
		this.DelegateRequest<Note?>(postRefBody);
		return this.DelegateResponse<PostRefActionResult>();
	}
}
