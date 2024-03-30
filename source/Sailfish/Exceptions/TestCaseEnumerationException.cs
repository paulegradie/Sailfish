using System;

namespace Sailfish.Exceptions;

public class TestCaseEnumerationException(Exception ex, string? message) : Exception(message, ex);