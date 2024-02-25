using MediatR;
using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseStartedNotification(TestInstanceContainerExternal TestInstanceContainer, IEnumerable<dynamic> TestCaseGroup) : INotification;
