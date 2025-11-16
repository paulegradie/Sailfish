using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseExceptionNotification : INotification
{
    public TestCaseExceptionNotification(TestInstanceContainerExternal? TestInstanceContainer,
        IEnumerable<dynamic> TestCaseGroup,
        Exception? Exception)
    {
        this.TestInstanceContainer = TestInstanceContainer;
        this.TestCaseGroup = TestCaseGroup;
        this.Exception = Exception;
    }

    public TestInstanceContainerExternal? TestInstanceContainer { get; init; }
    public IEnumerable<dynamic> TestCaseGroup { get; init; }
    public Exception? Exception { get; init; }

    public void Deconstruct(out TestInstanceContainerExternal? TestInstanceContainer, out IEnumerable<dynamic> TestCaseGroup, out Exception? Exception)
    {
        TestInstanceContainer = this.TestInstanceContainer;
        TestCaseGroup = this.TestCaseGroup;
        Exception = this.Exception;
    }
}