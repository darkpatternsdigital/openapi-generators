namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ParameterStyles;

public class FormExplodeController : QueryFormExplodeControllerBase
{
	protected override Task<GetFormExplodeActionResult> GetFormExplode(IEnumerable<int> ids)
	{
		this.DelegateRequest(ids?.ToArray());
		return this.DelegateResponse<GetFormExplodeActionResult>();
	}
}

public class FormNoExplodeController : QueryFormNoExplodeControllerBase
{
	protected override Task<GetFormNoExplodeActionResult> GetFormNoExplode(IEnumerable<int> ids)
	{
		this.DelegateRequest(ids?.ToArray());
		return this.DelegateResponse<GetFormNoExplodeActionResult>();
	}
}

public class SpaceDelimitedController : QuerySpaceDelimitedControllerBase
{
	protected override Task<GetSpaceDelimitedActionResult> GetSpaceDelimited(IEnumerable<int> ids)
	{
		this.DelegateRequest(ids?.ToArray());
		return this.DelegateResponse<GetSpaceDelimitedActionResult>();
	}
}

public class PipeDelimitedController : QueryPipeDelimitedControllerBase
{
	protected override Task<GetPipeDelimitedActionResult> GetPipeDelimited(IEnumerable<int> ids)
	{
		this.DelegateRequest(ids?.ToArray());
		return this.DelegateResponse<GetPipeDelimitedActionResult>();
	}
}

public class DeepObjectController : QueryDeepObjectControllerBase
{
	protected override Task<GetDeepObjectActionResult> GetDeepObject(Filter filter)
	{
		this.DelegateRequest<object?>(filter);
		return this.DelegateResponse<GetDeepObjectActionResult>();
	}
}

public class FormObjectController : QueryFormObjectControllerBase
{
	protected override Task<GetFormObjectActionResult> GetFormObject(Filter filter)
	{
		this.DelegateRequest<object?>(filter);
		return this.DelegateResponse<GetFormObjectActionResult>();
	}
}

public class AllowEmptyValueController : QueryAllowEmptyValueControllerBase
{
	protected override Task<GetAllowEmptyValueActionResult> GetAllowEmptyValue(string search)
	{
		this.DelegateRequest(search);
		return this.DelegateResponse<GetAllowEmptyValueActionResult>();
	}
}

public class AllowReservedController : QueryAllowReservedControllerBase
{
	protected override Task<GetAllowReservedActionResult> GetAllowReserved(string path)
	{
		this.DelegateRequest(path);
		return this.DelegateResponse<GetAllowReservedActionResult>();
	}
}

public class ContentParameterController : QueryContentControllerBase
{
	// The parameter's schema is in `content`, so it should bind as a Filter, but it is generated as `object`
	protected override Task<GetContentParameterActionResult> GetContentParameter(object filter)
	{
		this.DelegateRequest<object?>(filter);
		return this.DelegateResponse<GetContentParameterActionResult>();
	}
}

public class HeaderArrayController : HeaderSimpleControllerBase
{
	protected override Task<GetHeaderArrayActionResult> GetHeaderArray(IEnumerable<int> xIds)
	{
		this.DelegateRequest(xIds?.ToArray());
		return this.DelegateResponse<GetHeaderArrayActionResult>();
	}
}

public class PathSimpleArrayController : PathSimpleIdsControllerBase
{
	protected override Task<GetPathSimpleArrayActionResult> GetPathSimpleArray(IEnumerable<int> ids)
	{
		this.DelegateRequest(ids?.ToArray());
		return this.DelegateResponse<GetPathSimpleArrayActionResult>();
	}
}

public class PathLabelController : PathLabelIdControllerBase
{
	protected override Task<GetPathLabelActionResult> GetPathLabel(int id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<GetPathLabelActionResult>();
	}
}

public class PathMatrixController : PathMatrixIdControllerBase
{
	protected override Task<GetPathMatrixActionResult> GetPathMatrix(int id)
	{
		this.DelegateRequest(id);
		return this.DelegateResponse<GetPathMatrixActionResult>();
	}
}
