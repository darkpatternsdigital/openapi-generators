using Xunit;

namespace DarkPatterns.OpenApi.Transformations;

public class MediaTypesShould
{
	[Theory]
	[InlineData("", MediaTypeKind.None)]
	[InlineData("application/json", MediaTypeKind.Json)]
	[InlineData("APPLICATION/JSON", MediaTypeKind.Json)]
	[InlineData("application/json; charset=utf-8", MediaTypeKind.Json)]
	[InlineData("application/problem+json", MediaTypeKind.Json)]
	[InlineData("application/vnd.api+json", MediaTypeKind.Json)]
	[InlineData("text/plain", MediaTypeKind.Text)]
	[InlineData("text/csv", MediaTypeKind.Text)]
	[InlineData("text/*", MediaTypeKind.Text)]
	[InlineData("application/xml", MediaTypeKind.Raw)]
	[InlineData("application/octet-stream", MediaTypeKind.Raw)]
	[InlineData("image/png", MediaTypeKind.Raw)]
	[InlineData("image/*", MediaTypeKind.Raw)]
	[InlineData("*/*", MediaTypeKind.Raw)]
	public void Classify_response_media_types(string mediaType, MediaTypeKind expected)
	{
		Assert.Equal(expected, MediaTypes.Classify(mediaType));
	}
}
