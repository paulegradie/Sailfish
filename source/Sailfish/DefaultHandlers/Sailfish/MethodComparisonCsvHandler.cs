using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Handler for writing method comparison CSV files to disk.
/// This handler processes WriteMethodComparisonCsvNotification and creates CSV files
/// with session-based naming conventions.
/// </summary>
internal class MethodComparisonCsvHandler : INotificationHandler<WriteMethodComparisonCsvNotification>
{
    private readonly ILogger _logger;
    private readonly IRunSettings _runSettings;

    /// <summary>
    /// Initializes a new instance of the MethodComparisonCsvHandler class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="runSettings">The run settings for output configuration.</param>
    public MethodComparisonCsvHandler(ILogger logger, IRunSettings runSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runSettings = runSettings ?? throw new ArgumentNullException(nameof(runSettings));
    }

    /// <summary>
    /// Handles the WriteMethodComparisonCsvNotification by writing the CSV content to a file.
    /// </summary>
    /// <param name="notification">The CSV notification containing the content to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous file writing operation.</returns>
    public async Task Handle(WriteMethodComparisonCsvNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Debug,
                "Processing WriteMethodComparisonCsvNotification for '{0}'",
                notification.TestClassName);

            // Determine output directory
            var outputDirectory = !string.IsNullOrEmpty(notification.OutputDirectory)
                ? notification.OutputDirectory
                : _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.Log(LogLevel.Debug, "Created output directory: {0}", outputDirectory);
            }

            // Generate filename with session-based naming and timestamp
            var timestamp = notification.Timestamp.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{notification.TestClassName}_Results_{timestamp}.csv";
            
            // Apply tags if available
            if (_runSettings.Tags?.Count > 0)
            {
                fileName = DefaultFileSettings.AppendTagsToFilename(fileName, _runSettings.Tags);
            }

            var filePath = Path.Combine(outputDirectory, fileName);

            // Write CSV content to file
            await File.WriteAllTextAsync(filePath, notification.CsvContent, cancellationToken);

            _logger.Log(LogLevel.Information,
                "Successfully wrote method comparison CSV file: {0}", filePath);

            // Log file size for diagnostics
            var fileInfo = new FileInfo(filePath);
            _logger.Log(LogLevel.Debug,
                "CSV file size: {0} bytes, Lines: {1}",
                fileInfo.Length,
                notification.CsvContent.Split('\n').Length);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to write method comparison CSV file for '{0}': {1}",
                notification.TestClassName, ex.Message);
            throw;
        }
    }
}
