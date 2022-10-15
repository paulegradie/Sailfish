using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Program;

namespace Sailfish.Tool;

/// <summary>
/// The tool does not currently support additional registrations.
/// TODO: We can take an additional argument that points to
/// modules with additional registrations perhaps.
/// </summary>
internal class Program : SailfishProgramBase
{
    public static async Task Main(string[] args)
    {
        await SailfishMain<Program>(args);
    }

    protected override IEnumerable<Type> SourceTypesProvider()
    {
        return new[] { GetType() };
    }

    protected override void RegisterWithSailfish(ContainerBuilder builder)
    {
    }
}