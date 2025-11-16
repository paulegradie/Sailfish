using System;
using System.Security.Cryptography;
using System.Text;

namespace Sailfish.TestAdapter.Discovery;

internal class HashWrapper : IHashAlgorithm
{
    private readonly HashAlgorithm _hashAlgorithm = SHA1.Create();

    public Guid GuidFromString(string data)
    {
        var hash = _hashAlgorithm.ComputeHash(Encoding.Unicode.GetBytes(data));
        var b = new byte[16];
        Array.Copy(hash, b, 16);
        return new Guid(b);
    }
}