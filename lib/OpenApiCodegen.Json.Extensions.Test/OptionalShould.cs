﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;


public class OptionalShould
{
	public class HasOptional
	{
		[System.Text.Json.Serialization.JsonPropertyName("optionalNullableInteger")]
		[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
		public Optional<int?>? OptionalNullableInteger { get; set; }
	}

	[Fact]
	public void DeserializeOptionalMissing()
	{
		var json = @"{}";
		HasOptional target = JsonSerializer.Deserialize<HasOptional>(json)!;
		Assert.Null(target.OptionalNullableInteger);
	}

	[Fact]
	public void DeserializeOptionalAsNull()
	{
		var json = @"{ ""optionalNullableInteger"": null }";
		HasOptional target = JsonSerializer.Deserialize<HasOptional>(json)!;
		Assert.True(target.OptionalNullableInteger is Optional<int?>.Present { Value: null });
	}

	[Fact]
	public void DeserializeOptionalPresent()
	{
		var json = @"{ ""optionalNullableInteger"": 15 }";
		HasOptional target = JsonSerializer.Deserialize<HasOptional>(json)!;
		Assert.True(target.OptionalNullableInteger is Optional<int?>.Present { Value: 15 });
	}

	[Fact]
	public void SerializeOptionalMissing()
	{
		var expectedJson = @"{}";
		var original = new HasOptional { };
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	[Fact]
	public void SerializeOptionalAsNull()
	{
		var expectedJson = @"{""optionalNullableInteger"":null}";
		var original = new HasOptional { OptionalNullableInteger = new Optional<int?>.Present(null) };
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	[Fact]
	public void SerializeOptionalPresent()
	{
		var expectedJson = @"{""optionalNullableInteger"":15}";
		var original = new HasOptional { OptionalNullableInteger = new Optional<int?>.Present(15) };
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	public record RecordHasOptional(
		[property: System.Text.Json.Serialization.JsonPropertyName("optionalNullableInteger")]
		[property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
		Optional<int?>? OptionalNullableInteger
	);

	[Fact]
	public void DeserializeRecordOptionalMissing()
	{
		var json = @"{}";
		RecordHasOptional target = JsonSerializer.Deserialize<RecordHasOptional>(json)!;
		Assert.Null(target.OptionalNullableInteger);
	}

	[Fact]
	public void DeserializeRecordOptionalAsNull()
	{
		var json = @"{ ""optionalNullableInteger"": null }";
		RecordHasOptional target = JsonSerializer.Deserialize<RecordHasOptional>(json)!;
		Assert.True(target.OptionalNullableInteger is Optional<int?>.Present { Value: null });
	}

	[Fact]
	public void DeserializeRecordOptionalPresent()
	{
		var json = @"{ ""optionalNullableInteger"": 15 }";
		RecordHasOptional target = JsonSerializer.Deserialize<RecordHasOptional>(json)!;
		Assert.True(target.OptionalNullableInteger is Optional<int?>.Present { Value: 15 });
	}

	[Fact]
	public void SerializeRecordOptionalMissing()
	{
		var expectedJson = @"{}";
		var original = new RecordHasOptional(Optional<int?>.None);
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	[Fact]
	public void SerializeRecordOptionalAsNull()
	{
		var expectedJson = @"{""optionalNullableInteger"":null}";
		var original = new RecordHasOptional(new Optional<int?>.Present(null));
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	[Fact]
	public void SerializeRecordOptionalPresent()
	{
		var expectedJson = @"{""optionalNullableInteger"":15}";
		var original = new RecordHasOptional(new Optional<int?>.Present(15));
		var actual = JsonSerializer.Serialize(original);
		Assert.Equal(expectedJson, actual);
	}

	[Fact]
	public void AllowCreationOfOptional()
	{
		Optional<string> actual = Optional.Create("foo");
		Assert.True(actual is Optional<string>.Present);
		Assert.Equal("foo", Optional.GetValueOrThrow(actual));
	}

	[Fact]
	public void AllowUnwrappingOptional()
	{
		Optional<string> sample = Optional.Create("foo");
		var actual = Optional.GetValueOrThrow(sample);
		Assert.Equal("foo", actual);
	}

	[Fact]
	public void AllowPatternMatchingOfOptional()
	{
		Optional<string> sample = Optional.Create("foo");
		if (sample is { Value: var actual })
			Assert.Equal("foo", actual);
		else
			Assert.Fail("Pattern match failed");
	}

	[Fact]
	public void AllowPatternMatchingOfIOptional()
	{
		IOptional<string> sample = Optional.Create("foo");
		if (sample is { Value: var actual })
			Assert.Equal("foo", actual);
		else
			Assert.Fail("Pattern match failed");
	}

	[Fact]
	public void AllowCovarianceOfIOptional()
	{
		IOptional<object> sample = Optional.Create("foo");
		if (sample is { Value: var actual })
			Assert.Equal("foo", actual);
		else
			Assert.Fail("Pattern match failed");
	}

	[Fact]
	public void AllowCreationOfOptionalWithEnumerables()
	{
		Optional<IEnumerable<string>> actual = Optional.Create(Enumerable.Empty<string>());
		Assert.True(actual is Optional<IEnumerable<string>>.Present);
		Assert.Empty(Optional.GetValueOrThrow(actual));
	}

	[Fact]
	public void AllowUnwrappingOptionalWithEnumerables()
	{
		Optional<IEnumerable<string>> sample = Optional.Create(Enumerable.Empty<string>());
		var actual = Optional.GetValueOrThrow(sample);
		Assert.NotNull(actual);
		Assert.Empty(actual);
	}

}
