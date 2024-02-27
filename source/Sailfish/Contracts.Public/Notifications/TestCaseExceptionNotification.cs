using MediatR;
using Sailfish.Contracts.Public.Models;
using System;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Notifications;

public record TestCaseExceptionNotification(TestInstanceContainerExternal? TestInstanceContainer, IEnumerable<dynamic> TestCaseGroup, Exception? Exception) : INotification;