using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

internal record TestCaseDisabledNotification(
    TestInstanceContainerExternal TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup,
    bool DisableTheGroup)
    : INotification;