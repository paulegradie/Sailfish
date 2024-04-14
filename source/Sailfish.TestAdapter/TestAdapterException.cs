using System;

namespace Sailfish.TestAdapter;

public class TestAdapterException(string? message = null, Exception? exception = null) : Exception(message, exception);