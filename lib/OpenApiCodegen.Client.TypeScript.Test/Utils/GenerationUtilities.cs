using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Text.Json.Nodes;
using PrincipleStudios.OpenApiCodegen.TestUtils;

namespace PrincipleStudios.OpenApiCodegen.Client.TypeScript.Utils;

internal static class GenerationUtilities
{
	public static async Task<NodeUtility.ProcessResult> ConvertRequest(this CommonDirectoryFixture commonDirectory, string documentName, string operationName, object parameters)
	{
		await commonDirectory.PrepareOpenApiDirectory(documentName);

		var result = await commonDirectory.TsNode($@"
            import {{ AdapterRequestArgs }} from '@principlestudios/openapi-codegen-typescript';
            import {{ conversion }} from './{documentName}/operations/{operationName}';
            const request: AdapterRequestArgs = conversion.request({JsonSerializer.Serialize(parameters)});
            console.log(JSON.stringify(request));
        ");
		return result;
	}

	public static async Task<NodeUtility.ProcessResult> ConvertRequest(this CommonDirectoryFixture commonDirectory, string documentName, string operationName, object parameters, object body, string contentType = "application/json")
	{
		await commonDirectory.PrepareOpenApiDirectory(documentName);

		var result = await commonDirectory.TsNode($@"
            import {{ AdapterRequestArgs }} from '@principlestudios/openapi-codegen-typescript';
            import {{ conversion }} from './{documentName}/operations/{operationName}';
            const request: AdapterRequestArgs = conversion.request({JsonSerializer.Serialize(parameters)}, {JsonSerializer.Serialize(body)}, {JsonSerializer.Serialize(contentType)});
            console.log(JSON.stringify(request));
        ");
		return result;
	}

	public static async Task<NodeUtility.ProcessResult> CheckModel(this CommonDirectoryFixture commonDirectory, string documentName, string modelName, object body)
	{
		await commonDirectory.PrepareOpenApiDirectory(documentName);

		Assert.True(File.Exists(Path.Combine(commonDirectory.DirectoryPath, documentName, "models", $"{modelName}.ts")), "The model file does not exist");

		var result = await commonDirectory.TsNode($@"
            import {{ {modelName} }} from './{documentName}/models/{modelName}';
            const model: {modelName} = {JsonSerializer.Serialize(body)};
        ");
		return result;
	}

	// TODO - add extra headers to this
	public static async Task<NodeUtility.ProcessResult> ConvertResponse(this CommonDirectoryFixture commonDirectory, string documentName, string operationName, int statusCode, Optional<object?> body = default, string contentType = "application/json")
	{
		await commonDirectory.PrepareOpenApiDirectory(documentName);

		var result = await commonDirectory.TsNode($@"
            import {{ AdapterResponseArgs }} from '@principlestudios/openapi-codegen-typescript';
            import {{ conversion, Responses }} from './{documentName}/operations/{operationName}';
            const responseBody: Responses['data'] = {(body.HasValue ? JsonSerializer.Serialize(body.Value) : "undefined")};
            const response: AdapterResponseArgs = {{
                status: {statusCode},
                response: responseBody,
                getResponseHeader(headerName) {{
                    switch (headerName) {{
                        case 'Content-Type': return {JsonSerializer.Serialize(contentType)};
                        default: throw new Error('unknown header - TODO, support more');
                    }}
                }}
            }};
            const result: Responses = conversion.response(response);
            console.log(JSON.stringify(result));
        ");
		return result;
	}

	// TODO - add query string parameters to this
	public static JsonNode? AssertRequestSuccess(NodeUtility.ProcessResult result, string method, string path, Optional<object?> body = default, string contentType = "application/json")
	{
		Assert.Equal(0, result.ExitCode);

		var token = JsonNode.Parse(result.Output);
		Assert.Equal(method, token?["method"]?.GetValue<string>());
		Assert.Equal(path, token?["path"]?.GetValue<string>());
		if (body.HasValue)
		{
			Assert.Equal(contentType, token?["headers"]?["Content-Type"]?.GetValue<string>());
			CompareJson(token?["body"], body.Value);
		}
		else
		{
			Assert.Null(token?["headers"]?["Content-Type"]?.GetValue<string>());
			Assert.Null(token?["body"]);
		}
		return token;
	}

	public static JsonNode? AssertResponseSuccess(NodeUtility.ProcessResult result, int statusCode, Optional<object?> body = default)
	{
		Assert.Equal(0, result.ExitCode);

		var token = JsonNode.Parse(result.Output);
		Assert.Equal(statusCode, token?["statusCode"]?.GetValue<int>());
		if (body.HasValue) CompareJson(token?["data"], body.Value);
		return token;
	}

	public static JsonNode? AssertResponseOtherStatusCode(NodeUtility.ProcessResult result, Optional<object?> body = default)
	{
		Assert.Equal(0, result.ExitCode);

		var token = JsonNode.Parse(result.Output);
		Assert.Equal("other", token?["statusCode"]?.GetValue<string>());
		if (body.HasValue) CompareJson(token?["data"], body.Value);
		return token;
	}

	public static void CompareJson(JsonNode? actual, object? expected)
	{
		Assert.True(JsonCompare.CompareJson(actual, expected));
	}
}
