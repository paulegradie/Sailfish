using System;
using MediatR;

namespace Sailfish.Contracts.Private;

/// <summary>
/// Notification for generating CSV files from method comparison results.
/// This notification is published when method comparison tests with [WriteToCsv] attribute
/// complete execution and need to generate CSV output files.
/// </summary>
internal class WriteMethodComparisonCsvNotification : INotification
{
    /// <summary>
    /// Initializes a new instance of the WriteMethodComparisonCsvNotification class.
    /// </summary>
    /// <param name="testClassName">The name of the test class or session identifier.</param>
    /// <param name="csvContent">The CSV content to be written to the file.</param>
    /// <param name="outputDirectory">The output directory for the CSV file.</param>
    public WriteMethodComparisonCsvNotification(string testClassName, string csvContent, string outputDirectory)
    {
        TestClassName = testClassName ?? throw new ArgumentNullException(nameof(testClassName));
        CsvContent = csvContent ?? throw new ArgumentNullException(nameof(csvContent));
        OutputDirectory = outputDirectory ?? string.Empty;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the name of the test class or session identifier containing the comparison methods.
    /// </summary>
    public string TestClassName { get; set; }

    /// <summary>
    /// Gets the CSV content to be written to the file.
    /// </summary>
    public string CsvContent { get; set; }

    /// <summary>
    /// Gets the output directory where the CSV file should be created.
    /// If empty, the default output directory will be used.
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this notification was created.
    /// Used for generating unique filenames.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
