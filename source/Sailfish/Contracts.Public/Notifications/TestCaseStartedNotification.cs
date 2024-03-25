using System.Collections.Generic;
using MediatR;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseStartedNotification(TestInstanceContainerExternal TestInstanceContainer, IEnumerable<dynamic> TestCaseGroup) : INotification;
