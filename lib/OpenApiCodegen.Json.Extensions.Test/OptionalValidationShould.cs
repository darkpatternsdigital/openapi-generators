using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

public class OptionalValidationShould
{
	public record Model(
		[property: OptionalMinLength(2), OptionalMaxLength(4)] IOptional<string>? Name = null,
		[property: OptionalRegularExpression("^[A-Z]+$")] IOptional<string>? Code = null,
		[property: OptionalRange(1, 10)] IOptional<int>? Count = null
	);

	public static TheoryData<bool, Model> Cases => new()
	{
		{ true, new() },
		{ true, new(Name: Optional.Create("abc"), Code: Optional.Create("ABC"), Count: Optional.Create(5)) },
		{ false, new(Name: Optional.Create("a")) },
		{ false, new(Name: Optional.Create("abcde")) },
		{ false, new(Code: Optional.Create("abc")) },
		{ false, new(Count: Optional.Create(0)) },
		{ false, new(Count: Optional.Create(11)) },
	};

	[Theory]
	[MemberData(nameof(Cases))]
	public void Validate_present_values_and_allow_missing_values(bool expectedIsValid, Model model)
	{
		var results = new List<ValidationResult>();
		var actual = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

		Assert.Equal(expectedIsValid, actual);
	}

	[Fact]
	public void Remain_visible_as_the_standard_attributes()
	{
		Assert.IsAssignableFrom<MinLengthAttribute>(new OptionalMinLengthAttribute(1));
		Assert.IsAssignableFrom<MaxLengthAttribute>(new OptionalMaxLengthAttribute(1));
		Assert.IsAssignableFrom<RegularExpressionAttribute>(new OptionalRegularExpressionAttribute("."));
		Assert.IsAssignableFrom<RangeAttribute>(new OptionalRangeAttribute(1, 2));
	}
}
