namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Validation;

public class NumbersController : NumbersControllerBase
{
	protected override Task<GetNumbersActionResult> GetNumbers(int? multipleOfFive, int? atLeastOne, int? atMostTen, int? exclusive)
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetNumbersActionResult>();
	}
}

public class ArraysController : ArraysControllerBase
{
	protected override Task<PostArrayActionResult> PostArray(IEnumerable<string> postArrayBody)
	{
		this.DelegateRequest();
		return this.DelegateResponse<PostArrayActionResult>();
	}
}

public class ObjectsController : ObjectsControllerBase
{
	protected override Task<PostObjectActionResult> PostObject(Dictionary<string, string> postObjectBody)
	{
		this.DelegateRequest();
		return this.DelegateResponse<PostObjectActionResult>();
	}
}

public class PropertiesController : PropertiesControllerBase
{
	protected override Task<PostPropertiesActionResult> PostProperties(Measurements postPropertiesBody)
	{
		this.DelegateRequest();
		return this.DelegateResponse<PostPropertiesActionResult>();
	}
}
