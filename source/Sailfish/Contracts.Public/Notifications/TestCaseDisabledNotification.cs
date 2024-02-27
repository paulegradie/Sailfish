using MediatR;
using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

internal record TestCaseDisabledNotification(
    TestInstanceContainerExternal TestInstanceContainer,
    IEnumerable<dynamic> TestCaseGroup,
    bool DisableTheGroup) : INotification;
