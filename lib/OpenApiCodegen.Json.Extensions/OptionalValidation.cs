using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

/// <summary>
/// Validation attributes for <see cref="IOptional{T}"/> values. Each validates the present value
/// with its base attribute; a missing value is valid, as <c>null</c> is for the base attribute.
/// Subclassing keeps them visible to tools that look for the base attribute types.
/// </summary>
internal static class OptionalValidation
{
	public static object? Unwrap(object? value) =>
		value?.GetType().GetInterfaces().FirstOrDefault(IsOptionalInterface) is Type optionalInterface
			? optionalInterface.GetProperty(nameof(IOptional<object>.Value))!.GetValue(value)
			: value;

	private static bool IsOptionalInterface(Type type) =>
		type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOptional<>);
}

/// <inheritdoc cref="OptionalValidation"/>
public sealed class OptionalMinLengthAttribute(int length) : MinLengthAttribute(length)
{
	public override bool IsValid(object? value) => base.IsValid(OptionalValidation.Unwrap(value));
}

/// <inheritdoc cref="OptionalValidation"/>
public sealed class OptionalMaxLengthAttribute(int length) : MaxLengthAttribute(length)
{
	public override bool IsValid(object? value) => base.IsValid(OptionalValidation.Unwrap(value));
}

/// <inheritdoc cref="OptionalValidation"/>
public sealed class OptionalRegularExpressionAttribute(string pattern) : RegularExpressionAttribute(pattern)
{
	public override bool IsValid(object? value) => base.IsValid(OptionalValidation.Unwrap(value));
}

/// <inheritdoc cref="OptionalValidation"/>
public sealed class OptionalRangeAttribute : RangeAttribute
{
	public OptionalRangeAttribute(int minimum, int maximum) : base(minimum, maximum) { }
	public OptionalRangeAttribute(double minimum, double maximum) : base(minimum, maximum) { }

	public override bool IsValid(object? value) => base.IsValid(OptionalValidation.Unwrap(value));
}
