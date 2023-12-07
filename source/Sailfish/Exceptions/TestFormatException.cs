using System;

namespace Sailfish.Exceptions;

public class TestFormatException(string? message) : Exception(message)
{
}