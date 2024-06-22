using System;
using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseExceptionNotification(
    TestInstanceContainerExternal? TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup,
    Exception? Exception)
    : INotification;