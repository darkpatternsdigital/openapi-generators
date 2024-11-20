using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkPatterns.OpenApiCodegen.Json.Extensions;

public static class Optional
{
	public static Optional<T> Create<T>(T value) => new Optional<T>.Present(value);
	public static T GetValueOrThrow<T>(IOptional<T> input) =>
		input.TryGet(out var result) ? result : throw new InvalidOperationException();
}

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public interface IOptional<out T>
{
	T Value { get; }
}

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public abstract record Optional<T> : IOptional<T>
{
	private Optional() { }

	public static readonly Optional<T>? None = null;

	public abstract T Value { get; init; }

	public sealed record Present(T Value) : Optional<T>, IOptional<T>
	{
	}

	public class Serializer : JsonConverter<Optional<T>>
	{
		public override Optional<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var result = JsonSerializer.Deserialize<T>(ref reader, options)!;
			return new Present(result);
		}

		public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
		{
			if (value is Optional<T>.Present { Value: var v })
			{
				JsonSerializer.Serialize<T>(writer, v, options);
			}
			else
			{
				// This shouldn't happen! Optional should either be Present or be omitted via property serialization attributes
				throw new InvalidOperationException("Optional should either be Present or omitted via property serialization attributes: [global::System.Text.Json.Serialization.JsonIgnore(Condition = global::System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]");
			}
		}

		public override bool HandleNull => true;
	}
}

public class OptionalJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.IsGenericType
			&& (typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>) || typeToConvert.GetGenericTypeDefinition() == typeof(IOptional<>));
	}

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var genericArg = typeToConvert.GetGenericArguments()[0];

		return (JsonConverter)Activator.CreateInstance(
				typeof(Optional<>.Serializer).MakeGenericType(genericArg),
				BindingFlags.Instance | BindingFlags.Public,
				binder: null,
				args: Array.Empty<object>(),
				culture: null)!;
	}
}
