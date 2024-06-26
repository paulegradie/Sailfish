using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Tests.TestAdapter;

public class LoggerHelper : IMessageLogger
{
    public void SendMessage(TestMessageLevel testMessageLevel, string message)
    {
        Console.WriteLine($"{testMessageLevel}: {message}");
    }
}