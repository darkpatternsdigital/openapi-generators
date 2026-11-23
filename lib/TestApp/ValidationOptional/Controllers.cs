using DarkPatterns.OpenApiCodegen.Json.Extensions;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ValidationOptional;

public class JsonController : JsonControllerBase
{
	protected override Task<PostJsonActionResult> PostJson(OptionalValues postJsonBody)
	{
		this.DelegateRequest(postJsonBody);
		return this.DelegateResponse<PostJsonActionResult>();
	}
}

public class FormController : FormControllerBase
{
	protected override Task<PostFormActionResult> PostForm(IOptional<string> name, IOptional<string> code, IOptional<int> count, IOptional<string?> note)
	{
		this.DelegateRequest(new OptionalValues(name, code, count, note));
		return this.DelegateResponse<PostFormActionResult>();
	}
}
