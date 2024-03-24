using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Execution;

namespace Sailfish.Extensions.Methods;
internal static class TestCaseInstanceExtensionMethods
{
    public static bool IsDisabled(this TestInstanceContainerProvider testProvider)
    {
        return testProvider.Test.GetCustomAttributes<SailfishAttribute>().Single().Disabled;
    }
}
