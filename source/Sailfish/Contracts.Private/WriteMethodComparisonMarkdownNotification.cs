using System;
using MediatR;

namespace Sailfish.Contracts.Private;

/// <summary>
/// Notification for generating markdown files from method comparison results.
/// This notification is published when method comparison tests with [WriteToMarkdown] attribute
/// complete execution and need to generate markdown output files.
/// </summary>
internal class WriteMethodComparisonMarkdownNotification : INotification
{
    /// <summary>
    /// Gets the name of the test class containing the comparison methods.
    /// </summary>
    public string TestClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the markdown content to be written to the file.
    /// </summary>
    public string MarkdownContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets the output directory where the markdown file should be created.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the markdown generation was requested.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the WriteMethodComparisonMarkdownNotification class.
    /// </summary>
    /// <param name="testClassName">The name of the test class.</param>
    /// <param name="markdownContent">The markdown content to write.</param>
    /// <param name="outputDirectory">The output directory for the file.</param>
    public WriteMethodComparisonMarkdownNotification(string testClassName, string markdownContent, string outputDirectory)
    {
        TestClassName = testClassName ?? throw new ArgumentNullException(nameof(testClassName));
        MarkdownContent = markdownContent ?? throw new ArgumentNullException(nameof(markdownContent));
        OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Parameterless constructor for serialization support.
    /// </summary>
    public WriteMethodComparisonMarkdownNotification()
    {
    }
}
