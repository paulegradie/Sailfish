using System;
using System.Reflection;

namespace Tests.Common.Utils;

public static class ExceptionExtensions
{
    public static void SetStackTrace(this Exception exception, string stackTrace)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(stackTrace);

        var stackTraceField = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);

        if (stackTraceField != null)
            stackTraceField.SetValue(exception, stackTrace);
        else
            // As a fallback or warning, in case the reflection didn't work.
            // You could log this case or handle it according to your needs.
            Console.WriteLine("Warning: Unable to set stack trace via reflection.");
    }
}