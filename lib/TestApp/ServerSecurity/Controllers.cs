namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.ServerSecurity;

public class DocumentLevelController : DocumentLevelControllerBase
{
	protected override Task<GetDocumentLevelActionResult> GetDocumentLevel()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetDocumentLevelActionResult>();
	}
}

public class PublicController : PublicControllerBase
{
	protected override Task<GetPublicActionResult> GetPublic()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetPublicActionResult>();
	}
}

public class QueryKeyController : QueryKeyControllerBase
{
	protected override Task<GetQueryKeyActionResult> GetQueryKey()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetQueryKeyActionResult>();
	}
}

public class ApiKeyScopesController : ApiKeyScopesControllerBase
{
	protected override Task<GetApiKeyScopesActionResult> GetApiKeyScopes()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetApiKeyScopesActionResult>();
	}
}

public class CookieKeyController : CookieKeyControllerBase
{
	protected override Task<GetCookieKeyActionResult> GetCookieKey()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetCookieKeyActionResult>();
	}
}

public class BasicController : BasicControllerBase
{
	protected override Task<GetBasicActionResult> GetBasic()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetBasicActionResult>();
	}
}

public class BearerController : BearerControllerBase
{
	protected override Task<GetBearerActionResult> GetBearer()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetBearerActionResult>();
	}
}

public class OpenIdConnectController : OpenIdConnectControllerBase
{
	protected override Task<GetOpenIdConnectActionResult> GetOpenIdConnect()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetOpenIdConnectActionResult>();
	}
}

public class OAuthScopesController : OauthScopesControllerBase
{
	protected override Task<GetOAuthScopesActionResult> GetOAuthScopes()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetOAuthScopesActionResult>();
	}
}

public class AllOfController : AllOfControllerBase
{
	protected override Task<GetAllOfActionResult> GetAllOf()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetAllOfActionResult>();
	}
}

public class AnyOfController : AnyOfControllerBase
{
	protected override Task<GetAnyOfActionResult> GetAnyOf()
	{
		this.DelegateRequest();
		return this.DelegateResponse<GetAnyOfActionResult>();
	}
}
