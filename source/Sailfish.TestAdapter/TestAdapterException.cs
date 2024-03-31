using System;

internal class TestAdapterException(string? message = null, Exception? exception = null) : Exception(message, exception);