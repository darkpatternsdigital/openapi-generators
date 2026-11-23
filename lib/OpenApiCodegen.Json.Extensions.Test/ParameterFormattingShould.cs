using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

public class ParameterFormattingShould
{
	public enum Size { Small, Large }

	[TypeConverter(typeof(SizeWithValuesConverter))]
	public enum SizeWithValues { Small, Large }

	private class SizeWithValuesConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string);
		public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
			value switch { SizeWithValues.Small => "small", SizeWithValues.Large => "large", _ => throw new NotSupportedException() };
	}

	[Theory]
	[InlineData("de-DE")]
	[InlineData("en-US")]
	[InlineData("")]
	public void Format_values_the_same_way_in_every_culture(string culture)
	{
		var original = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
		try
		{
			Assert.Equal("1.5", ParameterFormatting.ToParameterString(1.5));
			Assert.Equal("1234567", ParameterFormatting.ToParameterString(1234567));
			Assert.Equal("2026-11-22T16:45:00.0000000-06:00", ParameterFormatting.ToParameterString(new DateTimeOffset(2026, 11, 22, 16, 45, 0, TimeSpan.FromHours(-6))));
			Assert.Equal("2026-11-22", ParameterFormatting.ToParameterString(new DateOnly(2026, 11, 22)));
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = original;
		}
	}

	[Fact]
	public void Format_booleans_in_lowercase()
	{
		Assert.Equal("true", ParameterFormatting.ToParameterString(true));
		Assert.Equal("false", ParameterFormatting.ToParameterString(false));
	}

	[Fact]
	public void Format_bytes_as_base64()
	{
		Assert.Equal("AQID", ParameterFormatting.ToParameterString(new byte[] { 1, 2, 3 }));
	}

	[Fact]
	public void Format_enums_with_their_type_converter()
	{
		// Generated enums have a TypeConverter that writes the OpenAPI value
		Assert.Equal("small", ParameterFormatting.ToParameterString(SizeWithValues.Small));
		Assert.Equal("Large", ParameterFormatting.ToParameterString(Size.Large));
	}

	[Fact]
	public void Keep_strings_and_guids_as_they_are()
	{
		Assert.Equal("a b/c", ParameterFormatting.ToParameterString("a b/c"));
		Assert.Equal("d3b07384-d9a0-4c9b-8f3e-5a1b2c3d4e5f", ParameterFormatting.ToParameterString(Guid.Parse("d3b07384-d9a0-4c9b-8f3e-5a1b2c3d4e5f")));
	}

	[Fact]
	public void Format_null_as_null()
	{
		Assert.Null(ParameterFormatting.ToParameterString(null));
	}
}
