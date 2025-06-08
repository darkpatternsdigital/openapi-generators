using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DarkPatterns.OpenApiCodegen.Client.TypeScript.Commands;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var commands = (from type in typeof(ICommandBase<>).Assembly.GetTypes()
						where type.IsAssignableTo(typeof(ICommandBase))
						let optionsType = (from iface in type.GetInterfaces()
										   where iface.IsGenericType
										   where iface.GetGenericTypeDefinition() == typeof(ICommandBase<>)
										   select iface.GetGenericArguments()[0]).FirstOrDefault()
						where optionsType != null
						select (Command: type, Options: optionsType))
			.ToDictionary(kvp => kvp.Options, kvp => kvp.Command);

		var result = new Parser(s =>
		{
			s.CaseSensitive = false;
			s.CaseInsensitiveEnumValues = true;
			s.AllowMultiInstance = true;
			s.AutoHelp = true;
			s.HelpWriter = Console.Error;
		}).ParseArguments((string[]?)args, [.. commands.Keys]);

		return await result
			.MapResult(async options =>
			{
				var commandType = commands[options.GetType()];

				var command = Activator.CreateInstance(commandType) as ICommandBase;

				return await command!.Run(options);
			},
			(_) =>
			{
				return Task.FromResult(1);
			});
	}

	public static string GetVersionInfo()
	{
		return $"{typeof(Program).Namespace} v{typeof(Program).Assembly.GetName().Version}";
	}
}
