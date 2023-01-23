using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IPropertySetGenerator
{
    IEnumerable<PropertySet> GeneratePropertySets(Type test);
}