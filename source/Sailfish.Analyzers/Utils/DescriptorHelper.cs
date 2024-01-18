using Microsoft.CodeAnalysis;

namespace Sailfish.Analyzers.Utils;

public static class DescriptorHelper
{
    public static DiagnosticDescriptor CreateDescriptor(
        DescriptorGroup group,
        int idValue,
        string title,
        string description,
        bool isEnabledByDefault,
        string messageFormat,
        DiagnosticSeverity severity = DiagnosticSeverity.Error,
        string? helpLinkUriAddendum = null)
    {
        return new DiagnosticDescriptor(
            $"SF{idValue}",
            title,
            messageFormat,
            group.Category,
            severity,
            isEnabledByDefault,
            helpLinkUri: $"{group.HelpLink} - {helpLinkUriAddendum}",
            description: description
        );
    }
}