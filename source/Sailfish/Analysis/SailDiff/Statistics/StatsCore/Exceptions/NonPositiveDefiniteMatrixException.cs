using System;
using System.Runtime.Serialization;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

[Serializable]
public class NonPositiveDefiniteMatrixException : Exception
{
    public NonPositiveDefiniteMatrixException()
    {
    }

    public NonPositiveDefiniteMatrixException(string message)
        : base(message)
    {
    }

    public NonPositiveDefiniteMatrixException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected NonPositiveDefiniteMatrixException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}