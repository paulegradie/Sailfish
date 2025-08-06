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
/// Handler for generating markdown files from method comparison results.
/// This handler processes WriteMethodComparisonMarkdownNotification to create
/// markdown files with professional formatting for method comparison tests.
/// </summary>
internal class MethodComparisonMarkdownHandler : INotificationHandler<WriteMethodComparisonMarkdownNotification>
{
    private readonly ILogger _logger;
    private readonly IRunSettings _runSettings;

    /// <summary>
    /// Initializes a new instance of the MethodComparisonMarkdownHandler class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="runSettings">The run settings for accessing output directory configuration.</param>
    public MethodComparisonMarkdownHandler(ILogger logger, IRunSettings runSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runSettings = runSettings ?? throw new ArgumentNullException(nameof(runSettings));
    }

    /// <summary>
    /// Handles the WriteMethodComparisonMarkdownNotification by creating a markdown file
    /// with the provided content and metadata.
    /// </summary>
    /// <param name="notification">The notification containing markdown content and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous file creation operation.</returns>
    public async Task Handle(WriteMethodComparisonMarkdownNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Information,
                "Generating method comparison markdown file for test class '{0}'",
                notification.TestClassName);

            // Generate filename with timestamp - support both class-based and session-based naming
            var timestamp = notification.Timestamp.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = notification.TestClassName.StartsWith("TestSession_")
                ? $"{notification.TestClassName}_MethodComparisons_{timestamp}.md"
                : $"{notification.TestClassName}_MethodComparisons_{timestamp}.md";

            // Use run settings to get the correct output directory
            var outputDirectory = _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.Log(LogLevel.Debug,
                    "Created output directory: {0}", outputDirectory);
            }

            // Create full file path
            var filePath = Path.Combine(outputDirectory, fileName);

            // Write markdown content to file
            await File.WriteAllTextAsync(filePath, notification.MarkdownContent, cancellationToken);

            // Set file as read-only to prevent accidental modification
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = true;
            }

            _logger.Log(LogLevel.Information, 
                "Method comparison markdown file created successfully: {0}", fileName);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to create method comparison markdown file for test class '{0}': {1}",
                notification.TestClassName, ex.Message);
            
            // Don't rethrow - we don't want markdown generation failures to break test execution
        }
    }
}
