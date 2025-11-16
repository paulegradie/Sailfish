using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

internal record TestCaseDisabledNotification : INotification
{
    public TestCaseDisabledNotification(TestInstanceContainerExternal TestInstanceContainer,
        IEnumerable<dynamic> TestCaseGroup,
        bool DisableTheGroup)
    {
        this.TestInstanceContainer = TestInstanceContainer;
        this.TestCaseGroup = TestCaseGroup;
        this.DisableTheGroup = DisableTheGroup;
    }

    public TestInstanceContainerExternal TestInstanceContainer { get; init; }
    public IEnumerable<dynamic> TestCaseGroup { get; init; }
    public bool DisableTheGroup { get; init; }

    public void Deconstruct(out TestInstanceContainerExternal TestInstanceContainer, out IEnumerable<dynamic> TestCaseGroup, out bool DisableTheGroup)
    {
        TestInstanceContainer = this.TestInstanceContainer;
        TestCaseGroup = this.TestCaseGroup;
        DisableTheGroup = this.DisableTheGroup;
    }
}