using DarkPatterns.OpenApiCodegen.Json.Extensions;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ParameterFormats;

public class FormatsController : FormatsNameControllerBase
{
	protected override Task<GetFormatsActionResult> GetFormats(string name, DateTimeOffset when, double amount, bool flag, Size size, byte[] data, Size xSize)
	{
		this.DelegateRequest((name, when, amount, flag, size, data, xSize));
		return this.DelegateResponse<GetFormatsActionResult>();
	}

	protected override Task<PostFormatsActionResult> PostFormats(string name, Size size, IOptional<DateTimeOffset> when, IOptional<bool> flag, IOptional<IEnumerable<string>> tags, IOptional<string?> note)
	{
		this.DelegateRequest(new FormatsForm(size, when, flag, tags, note));
		return this.DelegateResponse<PostFormatsActionResult>();
	}
}
