using System.Threading.Tasks;
using Autofac;
using Sailfish.Program;
using Sailfish.Tool.Framework.DIContainer;

namespace Sailfish.Tool
{
    /// <summary>
    /// The tool does currently support additional registrations.
    /// TODO: We can take an additional argument that points to
    /// modules with additional registrations perhaps.
    /// </summary>
    internal class Program : SailfishProgramBase
    {
        public static async Task Main(string[] args)
        {
            await SailfishMain<Program>(args);
        }

        public override async Task OnExecuteAsync()
        {
            await ContainerConfiguration
                .CompositionRoot()
                .Resolve<SailfishExecution>()
                .Run(AssembleRunRequest(), RegisterWithSailfish);
        }

        public void RegisterWithSailfish(ContainerBuilder builder)
        {
        }
    }
}