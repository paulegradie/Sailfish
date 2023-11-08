using System;

namespace Tests.Library.Utils;

public static class Some
{
    public static string RandomString()
    {
        return Guid.NewGuid().ToString();
    }
}