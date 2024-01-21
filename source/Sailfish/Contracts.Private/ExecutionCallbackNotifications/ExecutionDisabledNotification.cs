using MediatR;
using Sailfish.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sailfish.Contracts.Private.ExecutionCallbackHandlers;
internal class ExecutionDisabledNotification(TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup, bool disableTheGroup) : INotification
{
    public TestInstanceContainer TestInstanceContainer { get; set; } = testInstanceContainer;
    public IEnumerable<dynamic> TestCaseGroup { get; } = testCaseGroup;
    public bool DisableTheGroup { get; } = disableTheGroup;
}
