namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Responses;

public class RangesController : RangesControllerBase
{
	protected override Task<GetRangesActionResult> GetRanges()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetRangesActionResult>();
	}
}

public class DefaultController : DefaultControllerBase
{
	protected override Task<GetDefaultActionResult> GetDefault()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetDefaultActionResult>();
	}
}

public class MediaTypesController : MediaTypesControllerBase
{
	protected override Task<GetMediaTypesActionResult> GetMediaTypes()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetMediaTypesActionResult>();
	}
}

public class HeadersController : HeadersControllerBase
{
	protected override Task<GetHeadersActionResult> GetHeaders()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetHeadersActionResult>();
	}
}

public class RefController : RefControllerBase
{
	protected override Task<GetRefActionResult> GetRef()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetRefActionResult>();
	}
}
