using System.Threading.Tasks;

namespace DarkPatterns.OpenApiCodegen.Client.TypeScript.Commands;

public interface ICommandBase
{
	// A placeholder interface; do not implement directly
	Task<int> Run(object opts);
}

public interface ICommandBase<in TOptions> : ICommandBase
{
    Task<int> Run(TOptions opts);

    Task<int> ICommandBase.Run(object opts) => Run((TOptions)opts);
}
