using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using DarkPatterns.OpenApiCodegen.CSharp;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc
{

	public class SerializationShould : IClassFixture<TempDirectory>
	{
		private readonly string workingDirectory;

		public SerializationShould(TempDirectory directory)
		{
			workingDirectory = directory.DirectoryPath;
		}

		[Fact]
		public Task SerializeABasicClass() =>
			SerializeAsync(
				"petstore.yaml",
				@"new DPD.Controller.NewPet(Tag: DarkPatterns.OpenApiCodegen.Json.Extensions.Optional.Create(""dog""), Name: ""Fido"")",
				new { tag = "dog", name = "Fido" }
			);

		[Fact]
		public Task SerializeABasicClassWithOptionalValueOmitted() =>
			SerializeAsync(
				"petstore.yaml",
				@"new DPD.Controller.NewPet(Tag: DarkPatterns.OpenApiCodegen.Json.Extensions.Optional<string>.None, Name: ""Fido"")",
				new { name = "Fido" }
			);

		[Fact]
		public Task SerializeAnAllOfClass() =>
			SerializeAsync(
				"petstore.yaml",
				@"new DPD.Controller.Pet(Id: 1007L, Tag: DarkPatterns.OpenApiCodegen.Json.Extensions.Optional.Create(""dog""), Name: ""Fido"")",
				new { id = 1007L, tag = "dog", name = "Fido" }
			);

		[Fact]
		public Task SerializeAnAllOfClassWithOptionalValueOmitted() =>
			SerializeAsync(
				"petstore.yaml",
				@"new DPD.Controller.Pet(Id: 1007L, Tag: DarkPatterns.OpenApiCodegen.Json.Extensions.Optional<string>.None, Name: ""Fido"")",
				new { id = 1007L, name = "Fido" }
			);

		[Fact]
		public Task SerializeAnEnum() =>
			SerializeAsync(
				"enum.yaml",
				@"DPD.Controller.Option.Rock",
				"rock"
			);

		[Fact]
		public Task DeserializeABasicClass() =>
			DeserializeAsync(
				"petstore.yaml",
				new { tag = (string?)null, name = "Fido" },
				"DPD.Controller.NewPet"
			);

		[Fact]
		public Task DeserializeAnAllOfClass() =>
			DeserializeAsync(
				"petstore.yaml",
				new { id = 1007L, tag = (string?)null, name = "Fido" },
				"DPD.Controller.Pet"
			);

		[Fact]
		public Task DeserializeAnEnum() =>
			DeserializeAsync(
				"enum.yaml",
				"rock",
				"DPD.Controller.Option"
			);

		[Theory]
		[InlineData("new DPD.Controller.Pet(Dog: new DPD.Controller.Dog(Bark: true, Breed: \"Shiba Inu\"))", "{ \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("new DPD.Controller.Pet(Cat: new DPD.Controller.Cat(Hunts: false, Age: 12))", "{ \"hunts\": false, \"age\": 12 }")]
		[InlineData("new DPD.Controller.SpecifiedPet(Dog: new DPD.Controller.Dog(Bark: true, Breed: \"Shiba Inu\"))", "{ \"petType\": \"dog\", \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("new DPD.Controller.SpecifiedPet(Cat: new DPD.Controller.Cat(Hunts: false, Age: 12))", "{ \"petType\": \"cat\", \"hunts\": false, \"age\": 12 }")]
		public Task SerializeAOneOfObject(string csharpScript, string json) =>
			SerializeJsonAsync(
				"one-of.yaml",
				csharpScript,
				json
			);

		[Theory]
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("DPD.Controller.Pet", "{ \"hunts\": false, \"age\": 12 }")]
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": \"dog\", \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": \"cat\", \"hunts\": false, \"age\": 12 }")]
		public Task DeserializeAOneOfObject(string csharpType, string json) =>
			DeserializeJsonAsync(
				"one-of.yaml",
				json,
				csharpType
			);

		[Theory]
		// Both cases are valid, so it isn't exactly one of them
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true, \"breed\": \"Shiba Inu\", \"hunts\": false, \"age\": 12 }")]
		// Neither case is valid
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true }")]
		// Not an object, so there's no discriminator to read
		[InlineData("DPD.Controller.SpecifiedPet", "\"dog\"")]
		// No discriminator
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		// A discriminator that isn't a string
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": 5, \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		// An unknown discriminator
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": \"fish\" }")]
		public async Task ThrowJsonExceptionsForInvalidOneOfObjects(string csharpType, string json)
		{
			var (exceptionType, _, message) = await DeserializeFailureAsync("one-of.yaml", "[" + json + "]", $"System.Collections.Generic.List<{csharpType}>");

			Assert.Equal("System.Text.Json.JsonException", exceptionType);
			Assert.DoesNotContain("{position}", message);
		}

		[Fact]
		public async Task ReportTheLocationOfTheOneOfObjectAndTheErrorInsideIt()
		{
			var (_, path, message) = await DeserializeFailureAsync(
				"one-of.yaml",
				"[{ \"petType\": \"dog\", \"bark\": \"yes\", \"breed\": \"Shiba Inu\" }]",
				"System.Collections.Generic.List<DPD.Controller.SpecifiedPet>"
			);

			Assert.Equal("$[0]", path);
			Assert.Contains("$.bark", message);
		}

		[Theory]
		[InlineData("new DPD.Controller.IdOrName(_0: 5L)", "5")]
		[InlineData("new DPD.Controller.IdOrName(_1: \"Fido\")", "\"Fido\"")]
		[InlineData("new DPD.Controller.Pet()", "null")]
		public Task SerializeOneOfValuesThatAreNotObjects(string csharpScript, string json) =>
			SerializeJsonAsync(
				"one-of.yaml",
				csharpScript,
				json
			);

		[Theory]
		[InlineData("DPD.Controller.IdOrName", "5")]
		[InlineData("DPD.Controller.IdOrName", "\"Fido\"")]
		public Task DeserializeOneOfValuesThatAreNotObjects(string csharpType, string json) =>
			DeserializeJsonAsync(
				"one-of.yaml",
				json,
				csharpType
			);

		[Theory]
		[InlineData("DPD.Controller.Pet", true)]
		[InlineData("DPD.Controller.IdOrName", true)]
		[InlineData("DPD.Controller.DeclaredPet", true)]
		// Union serialization wouldn't write the discriminator, which its cases don't declare
		[InlineData("DPD.Controller.SpecifiedPet", false)]
		public void UseJsonUnionsForOneOfWhenEnabled(string csharpType, bool expected)
		{
			var sources = DynamicCompilation.GetGeneratedSources("one-of.yaml", jsonUnions: true);
			var source = sources[csharpType + ".cs"];
			Assert.Equal(expected, source.Contains("[global::System.Runtime.CompilerServices.Union]"));
		}

		[Fact]
		public void UseTheJsonConverterForOneOfByDefault()
		{
			var sources = DynamicCompilation.GetGeneratedSources("one-of.yaml");
			Assert.DoesNotContain("[global::System.Runtime.CompilerServices.Union]", sources["DPD.Controller.Pet.cs"]);
		}

#if NET11_0_OR_GREATER
		[Theory]
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("DPD.Controller.Pet", "{ \"hunts\": false, \"age\": 12 }")]
		[InlineData("DPD.Controller.IdOrName", "5")]
		[InlineData("DPD.Controller.IdOrName", "\"Fido\"")]
		[InlineData("DPD.Controller.DeclaredPet", "{ \"petType\": \"dog\", \"bark\": true }")]
		[InlineData("DPD.Controller.DeclaredPet", "{ \"petType\": \"cat\", \"hunts\": false }")]
		public Task RoundTripOneOfJsonUnions(string csharpType, string json) =>
			DeserializeJsonAsync("one-of.yaml", json, csharpType, jsonUnions: true);

		[Theory]
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true, \"breed\": \"Shiba Inu\", \"hunts\": false, \"age\": 12 }")]
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true }")]
		[InlineData("DPD.Controller.DeclaredPet", "\"dog\"")]
		[InlineData("DPD.Controller.DeclaredPet", "{ \"bark\": true }")]
		[InlineData("DPD.Controller.DeclaredPet", "{ \"petType\": \"fish\" }")]
		public async Task ThrowJsonExceptionsForInvalidOneOfJsonUnions(string csharpType, string json)
		{
			var (exceptionType, path, _) = await DeserializeFailureAsync("one-of.yaml", "[" + json + "]", $"System.Collections.Generic.List<{csharpType}>", jsonUnions: true);

			Assert.Equal("System.Text.Json.JsonException", exceptionType);
			Assert.Equal("$[0]", path);
		}

		[Fact]
		public async Task ReportTheFullPathOfErrorsInsideOneOfJsonUnions()
		{
			var (_, path, _) = await DeserializeFailureAsync(
				"one-of.yaml",
				"[{ \"petType\": \"dog\", \"bark\": \"yes\" }]",
				"System.Collections.Generic.List<DPD.Controller.DeclaredPet>",
				jsonUnions: true
			);

			Assert.Equal("$[0].bark", path);
		}
#endif

		private async Task<(string ExceptionType, string? Path, string Message)> DeserializeFailureAsync(string documentName, string json, string typeName, bool jsonUnions = false)
		{
			var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName, jsonUnions);

			var fullPath = Path.Combine(workingDirectory, Path.GetRandomFileName());
			File.WriteAllBytes(fullPath, libBytes);

			var scriptOptions = ScriptOptions.Default
				.AddReferences(DynamicCompilation.SystemTextCompilationRefPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray())
				.AddReferences(MetadataReference.CreateFromFile(fullPath));

			var script = @$"
				try
				{{
					System.Text.Json.JsonSerializer.Deserialize<{typeName}>(@""{json.Replace("\"", "\"\"")}"");
					return new string[] {{ ""none"", null, """" }};
				}}
				catch (System.Exception ex)
				{{
					return new string[] {{ ex.GetType().FullName, (ex as System.Text.Json.JsonException)?.Path, ex.Message }};
				}}";

			var result = (string?[])await CSharpScript.EvaluateAsync(script, scriptOptions);
			return (result[0]!, result[1], result[2]!);
		}

		private Task SerializeAsync(string documentName, string csharpInitialization, object comparisonObject) =>
			SerializeJsonAsync(documentName, csharpInitialization, System.Text.Json.JsonSerializer.Serialize(comparisonObject));

		private async Task SerializeJsonAsync(string documentName, string csharpInitialization, string comparisonJson, bool jsonUnions = false)
		{
			var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName, jsonUnions);

			var fullPath = Path.Combine(workingDirectory, Path.GetRandomFileName());
			File.WriteAllBytes(fullPath, libBytes);

			var scriptOptions = ScriptOptions.Default
				.AddReferences(DynamicCompilation.SystemTextCompilationRefPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray())
				.AddReferences(MetadataReference.CreateFromFile(fullPath));

			var result = (string)await CSharpScript.EvaluateAsync($"System.Text.Json.JsonSerializer.Serialize({csharpInitialization})", scriptOptions);

			Assert.True(JsonCompare.CompareJsonStrings(result, comparisonJson));
		}

		private Task DeserializeAsync(string documentName, object targetObect, string typeName) =>
			DeserializeJsonAsync(documentName, System.Text.Json.JsonSerializer.Serialize(targetObect), typeName);

		private async Task DeserializeJsonAsync(string documentName, string targetJson, string typeName, bool jsonUnions = false)
		{
			var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName, jsonUnions);

			var fullPath = Path.Combine(workingDirectory, Path.GetRandomFileName());
			File.WriteAllBytes(fullPath, libBytes);

			var scriptOptions = ScriptOptions.Default
				.AddReferences(DynamicCompilation.SystemTextCompilationRefPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray())
				.AddReferences(MetadataReference.CreateFromFile(fullPath));

			var originalJson = targetJson.Replace("\"", "\"\"");

			var script = @$"
                System.Text.Json.JsonSerializer.Serialize(
                    System.Text.Json.JsonSerializer.Deserialize<{typeName}>(
                        @""{originalJson}""
                    )
                )";

			var result = (string)await CSharpScript.EvaluateAsync(script, scriptOptions);

			Assert.True(JsonCompare.CompareJsonStrings(result, targetJson));
		}
	}
}
