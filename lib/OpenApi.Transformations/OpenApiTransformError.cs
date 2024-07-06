using System;

namespace PrincipleStudios.OpenApi.Transformations
{
	[Obsolete("Use a DiagnosticBase instance instead")]
	public class OpenApiTransformError
	{
		public OpenApiTransformError(OpenApiContext context, string message)
		{
			Context = context;
			Message = message;
		}

		public OpenApiContext Context { get; set; }
		public string Message { get; set; }

		public override string ToString()
		{
			return $"{Context.ToOpenApiPathContextString()}: {Message}";
		}
	}
}