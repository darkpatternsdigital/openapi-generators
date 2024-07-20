using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using DarkPatterns.OpenApiCodegen.TestUtils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
		[InlineData("new DPD.Controller.SpecifiedPet(Dog: new DPD.Controller.Dog(Bark: true, Breed: \"Shiba Inu\"))", "{ \"petType\": \"Dog\", \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("new DPD.Controller.SpecifiedPet(Cat: new DPD.Controller.Cat(Hunts: false, Age: 12))", "{ \"petType\": \"Cat\", \"hunts\": false, \"age\": 12 }")]
		public Task SerializeAOneOfObject(string csharpScript, string json) =>
			SerializeJsonAsync(
				"one-of.yaml",
				csharpScript,
				json
			);

		[Theory]
		[InlineData("DPD.Controller.Pet", "{ \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("DPD.Controller.Pet", "{ \"hunts\": false, \"age\": 12 }")]
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": \"Dog\", \"bark\": true, \"breed\": \"Shiba Inu\" }")]
		[InlineData("DPD.Controller.SpecifiedPet", "{ \"petType\": \"Cat\", \"hunts\": false, \"age\": 12 }")]
		public Task DeserializeAOneOfObject(string csharpType, string json) =>
			DeserializeJsonAsync(
				"one-of.yaml",
				json,
				csharpType
			);

		private Task SerializeAsync(string documentName, string csharpInitialization, object comparisonObject) =>
			SerializeJsonAsync(documentName, csharpInitialization, System.Text.Json.JsonSerializer.Serialize(comparisonObject));

		private async Task SerializeJsonAsync(string documentName, string csharpInitialization, string comparisonJson)
		{
			var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName);

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

		private async Task DeserializeJsonAsync(string documentName, string targetJson, string typeName)
		{
			var libBytes = DynamicCompilation.GetGeneratedLibrary(documentName);

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
