using System;
using System.Runtime.Serialization;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

[Serializable]
public class SingularMatrixException : Exception
{
    public SingularMatrixException()
    {
    }

    public SingularMatrixException(string message)
        : base(message)
    {
    }

    public SingularMatrixException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected SingularMatrixException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}