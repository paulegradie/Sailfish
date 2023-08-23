using System;
using System.Security.Cryptography;
using System.Text;

namespace Sailfish.TestAdapter.Discovery;

internal class HashWrapper : IHashAlgorithm
{
    private readonly HashAlgorithm hashAlgorithm;

    public HashWrapper()
    {
        hashAlgorithm = SHA1.Create();
    }

    private byte[] ComputeHash(byte[] buffer)
    {
        return hashAlgorithm.ComputeHash(buffer);
    }

    public Guid GuidFromString(string data)
    {
        var hash = hashAlgorithm.ComputeHash(Encoding.Unicode.GetBytes(data));
        var b = new byte[16];
        Array.Copy(hash, b, 16);
        return new Guid(b);
    }
}