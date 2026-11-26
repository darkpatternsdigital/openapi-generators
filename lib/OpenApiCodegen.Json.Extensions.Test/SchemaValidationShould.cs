using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

public class SchemaValidationShould
{
	public record Item(string Name);

	public record Model(
		[property: MultipleOf(0.5)] decimal? Half = null,
		[property: MultipleOf(5)] IOptional<int>? Five = null,
		[property: UniqueItems] IEnumerable<Item>? Items = null,
		[property: MinProperties(1), MaxProperties(2)] Dictionary<string, string>? Map = null,
		[property: MinProperties(1)] IOptional<Item?>? Object = null,
		[property: Minimum(1)] int? AtLeastOne = null,
		[property: Minimum(0, Exclusive = true), Maximum(10, Exclusive = true)] IOptional<double>? Between = null,
		[property: Maximum(10)] long? AtMostTen = null
	);

	public static TheoryData<bool, Model> Cases => new()
	{
		{ true, new() },
		{ true, new(Half: 1.5m, Five: Optional.Create(10), Items: [new("a"), new("b")], Map: new() { ["a"] = "1" }, Object: Optional.Create<Item?>(new("a"))) },
		{ false, new(Half: 1.25m) },
		{ false, new(Five: Optional.Create(7)) },
		{ false, new(Items: [new("a"), new("a")]) },
		{ false, new(Map: []) },
		{ false, new(Map: new() { ["a"] = "1", ["b"] = "2", ["c"] = "3" }) },
		{ false, new(Object: Optional.Create<Item?>(new(null!))) },
		{ true, new(AtLeastOne: 1, Between: Optional.Create(0.5), AtMostTen: 10) },
		{ false, new(AtLeastOne: 0) },
		{ false, new(Between: Optional.Create(0.0)) },
		{ false, new(Between: Optional.Create(10.0)) },
		{ false, new(AtMostTen: 11) },
		{ false, new(AtMostTen: long.MaxValue) },
	};

	[Theory]
	[InlineData(false, "The field X must be at least 1.")]
	[InlineData(true, "The field X must be greater than 1.")]
	public void Describe_the_minimum(bool exclusive, string expected) =>
		Assert.Equal(expected, new MinimumAttribute(1) { Exclusive = exclusive }.FormatErrorMessage("X"));

	[Theory]
	[InlineData(false, "The field X must be at most 10.")]
	[InlineData(true, "The field X must be less than 10.")]
	public void Describe_the_maximum(bool exclusive, string expected) =>
		Assert.Equal(expected, new MaximumAttribute(10) { Exclusive = exclusive }.FormatErrorMessage("X"));

	[Theory]
	[MemberData(nameof(Cases))]
	public void Validate_present_values_and_allow_missing_values(bool expectedIsValid, Model model)
	{
		var results = new List<ValidationResult>();
		var actual = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

		Assert.Equal(expectedIsValid, actual);
	}
}
