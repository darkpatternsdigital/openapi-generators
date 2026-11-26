namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Models;

// Each operation echoes its request body, so tests can check that values round-trip.
// The test's response is only used to count the call.

public class FormatsController : FormatsControllerBase
{
	protected override async Task<EchoFormatsActionResult> EchoFormats(Formats echoFormatsBody)
	{
		this.DelegateRequest<Formats?>(echoFormatsBody);
		await this.DelegateResponse<EchoFormatsActionResult>();
		return EchoFormatsActionResult.Ok(echoFormatsBody);
	}
}

public class ReadWriteController : ReadWriteControllerBase
{
	protected override async Task<EchoReadWriteActionResult> EchoReadWrite(ReadWrite echoReadWriteBody)
	{
		this.DelegateRequest<ReadWrite?>(echoReadWriteBody);
		return await this.DelegateResponse<EchoReadWriteActionResult>();
	}
}

public class OpenObjectController : OpenObjectControllerBase
{
	protected override async Task<EchoOpenObjectActionResult> EchoOpenObject(OpenObject echoOpenObjectBody)
	{
		this.DelegateRequest<OpenObject?>(echoOpenObjectBody);
		await this.DelegateResponse<EchoOpenObjectActionResult>();
		return EchoOpenObjectActionResult.Ok(echoOpenObjectBody);
	}
}

public class ClosedObjectController : ClosedObjectControllerBase
{
	protected override async Task<EchoClosedObjectActionResult> EchoClosedObject(ClosedObject echoClosedObjectBody)
	{
		this.DelegateRequest<ClosedObject?>(echoClosedObjectBody);
		await this.DelegateResponse<EchoClosedObjectActionResult>();
		return EchoClosedObjectActionResult.Ok(echoClosedObjectBody);
	}
}

public class TypedAdditionalPropertiesController : TypedAdditionalPropertiesControllerBase
{
	protected override async Task<EchoTypedAdditionalPropertiesActionResult> EchoTypedAdditionalProperties(TypedAdditionalProperties echoTypedAdditionalPropertiesBody)
	{
		this.DelegateRequest<TypedAdditionalProperties?>(echoTypedAdditionalPropertiesBody);
		await this.DelegateResponse<EchoTypedAdditionalPropertiesActionResult>();
		return EchoTypedAdditionalPropertiesActionResult.Ok(echoTypedAdditionalPropertiesBody);
	}
}

public class MatrixController : MatrixControllerBase
{
	protected override async Task<EchoMatrixActionResult> EchoMatrix(Matrix echoMatrixBody)
	{
		this.DelegateRequest<Matrix?>(echoMatrixBody);
		await this.DelegateResponse<EchoMatrixActionResult>();
		return EchoMatrixActionResult.Ok(echoMatrixBody);
	}
}

public class DefaultsController : DefaultsControllerBase
{
	protected override async Task<EchoDefaultsActionResult> EchoDefaults(Defaults echoDefaultsBody)
	{
		this.DelegateRequest<Defaults?>(echoDefaultsBody);
		await this.DelegateResponse<EchoDefaultsActionResult>();
		return EchoDefaultsActionResult.Ok(echoDefaultsBody);
	}
}

public class TreeController : TreeControllerBase
{
	protected override async Task<EchoTreeActionResult> EchoTree(TreeNode echoTreeBody)
	{
		this.DelegateRequest<TreeNode?>(echoTreeBody);
		await this.DelegateResponse<EchoTreeActionResult>();
		return EchoTreeActionResult.Ok(echoTreeBody);
	}
}
