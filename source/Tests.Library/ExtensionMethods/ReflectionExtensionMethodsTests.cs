using Sailfish.Attributes;
using Sailfish.Extensions.Methods;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Library.ExtensionMethods;

public class ReflectionExtensionMethodsTests
{
    [Fact]
    public void WhenExtendingABaseClass_WhereOnlyTheBaseClassHasTheSailfishGlobalSetupAttribute_ThenOnlyOneMethodWillBeFound()
    {
        var methods = new ExtendsBaseExample().FindMethodsDecoratedWithAttribute<SailfishGlobalSetupAttribute>();
        methods.Count.ShouldBe(1);
    }
    
    [Fact]
    public void WhenExtendingAClassThatExtendsTheBaseClass_WhereOnlyTheBaseClassHasTheSailfishGlobalSetupAttribute_ThenOnlyOneMethodWillBeFound()
    {
        var methods = new ExtendsExtendsBaseExample().FindMethodsDecoratedWithAttribute<SailfishGlobalSetupAttribute>();
        methods.Count.ShouldBe(1);
    }

    public class BaseExample
    {
        [SailfishGlobalSetup]
        public async Task EnforcedGlobalSetup(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }

    public class ExtendsBaseExample : BaseExample;
    
    public class ExtendsExtendsBaseExample : ExtendsBaseExample;

}