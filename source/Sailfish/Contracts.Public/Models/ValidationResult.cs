using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Contracts.Public.Models;

public class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationWarning> warnings)
    {
        Warnings = warnings?.ToList() ?? new List<ValidationWarning>();
    }

    public IReadOnlyList<ValidationWarning> Warnings { get; }

    public bool HasWarnings => Warnings.Count > 0;
}

