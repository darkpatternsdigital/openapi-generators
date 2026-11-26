using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

/// <summary>
/// Validation attributes for JSON Schema keywords that have no standard attribute. Each also
/// validates the present value of an <see cref="IOptional{T}"/>; <c>null</c> and missing values
/// are valid.
/// </summary>
internal static class SchemaValidation
{
	public static int CountProperties(object value) =>
		value switch
		{
			IEnumerable entries => entries.Cast<object?>().Count(),
			_ => value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Count(p => p.GetIndexParameters().Length == 0 && p.GetValue(value) != null),
		};

	public static int CompareTo(object value, double bound)
	{
		try
		{
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture).CompareTo((decimal)bound);
		}
		catch (OverflowException)
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture).CompareTo(bound);
		}
	}
}

/// <summary>Validates the JSON Schema <c>minimum</c> keyword, and OpenAPI 3.0's boolean <c>exclusiveMinimum</c></summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinimumAttribute(double minimum) : ValidationAttribute
{
	public double Minimum => minimum;
	public bool Exclusive { get; set; }

	public override bool IsValid(object? value)
	{
		if (OptionalValidation.Unwrap(value) is not { } actual)
			return true;
		var comparison = SchemaValidation.CompareTo(actual, minimum);
		return Exclusive ? comparison > 0 : comparison >= 0;
	}

	public override string FormatErrorMessage(string name) =>
		string.Format(CultureInfo.CurrentCulture, ErrorMessage ?? (Exclusive ? "The field {0} must be greater than {1}." : "The field {0} must be at least {1}."), name, minimum);
}

/// <summary>Validates the JSON Schema <c>maximum</c> keyword, and OpenAPI 3.0's boolean <c>exclusiveMaximum</c></summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaximumAttribute(double maximum) : ValidationAttribute
{
	public double Maximum => maximum;
	public bool Exclusive { get; set; }

	public override bool IsValid(object? value)
	{
		if (OptionalValidation.Unwrap(value) is not { } actual)
			return true;
		var comparison = SchemaValidation.CompareTo(actual, maximum);
		return Exclusive ? comparison < 0 : comparison <= 0;
	}

	public override string FormatErrorMessage(string name) =>
		string.Format(CultureInfo.CurrentCulture, ErrorMessage ?? (Exclusive ? "The field {0} must be less than {1}." : "The field {0} must be at most {1}."), name, maximum);
}

/// <summary>Validates the JSON Schema <c>multipleOf</c> keyword</summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MultipleOfAttribute(double divisor) : ValidationAttribute("The field {0} must be a multiple of {1}.")
{
	public double Divisor => divisor;

	public override bool IsValid(object? value)
	{
		if (OptionalValidation.Unwrap(value) is not { } actual)
			return true;
		try
		{
			return Convert.ToDecimal(actual, CultureInfo.InvariantCulture) % (decimal)divisor == 0;
		}
		catch (OverflowException)
		{
			var quotient = Convert.ToDouble(actual, CultureInfo.InvariantCulture) / divisor;
			return Math.Floor(quotient) == quotient;
		}
	}

	public override string FormatErrorMessage(string name) =>
		string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, divisor);
}

/// <summary>Validates the JSON Schema <c>uniqueItems</c> keyword, comparing items by their JSON</summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class UniqueItemsAttribute() : ValidationAttribute("The field {0} must not contain duplicate items.")
{
	public override bool IsValid(object? value)
	{
		if (OptionalValidation.Unwrap(value) is not IEnumerable items || items is string)
			return true;
		var seen = new HashSet<string>();
		foreach (var item in items)
		{
			if (!seen.Add(JsonSerializer.Serialize(item, item?.GetType() ?? typeof(object))))
				return false;
		}
		return true;
	}
}

/// <summary>Validates the JSON Schema <c>minProperties</c> keyword</summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinPropertiesAttribute(int count) : ValidationAttribute("The field {0} must have at least {1} properties.")
{
	public int Count => count;

	public override bool IsValid(object? value) =>
		OptionalValidation.Unwrap(value) is not { } actual || SchemaValidation.CountProperties(actual) >= count;

	public override string FormatErrorMessage(string name) =>
		string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, count);
}

/// <summary>Validates the JSON Schema <c>maxProperties</c> keyword</summary>
/// <inheritdoc cref="SchemaValidation"/>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxPropertiesAttribute(int count) : ValidationAttribute("The field {0} must have at most {1} properties.")
{
	public int Count => count;

	public override bool IsValid(object? value) =>
		OptionalValidation.Unwrap(value) is not { } actual || SchemaValidation.CountProperties(actual) <= count;

	public override string FormatErrorMessage(string name) =>
		string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, count);
}
