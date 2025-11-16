using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseStartedNotification : INotification
{
    public TestCaseStartedNotification(TestInstanceContainerExternal TestInstanceContainer,
        IEnumerable<dynamic> TestCaseGroup)
    {
        this.TestInstanceContainer = TestInstanceContainer;
        this.TestCaseGroup = TestCaseGroup;
    }

    public TestInstanceContainerExternal TestInstanceContainer { get; init; }
    public IEnumerable<dynamic> TestCaseGroup { get; init; }

    public void Deconstruct(out TestInstanceContainerExternal TestInstanceContainer, out IEnumerable<dynamic> TestCaseGroup)
    {
        TestInstanceContainer = this.TestInstanceContainer;
        TestCaseGroup = this.TestCaseGroup;
    }
}