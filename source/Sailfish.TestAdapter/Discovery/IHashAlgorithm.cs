using System;

namespace Sailfish.TestAdapter.Discovery;

public interface IHashAlgorithm
{
    Guid GuidFromString(string data);
}