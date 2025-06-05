using FileSyncLibrary;

// Example usage of the FileSyncLibrary

namespace FileSyncDemo;

public static class Examples
{
    /// <summary>
    /// Basic file synchronization example
    /// </summary>
    public static async Task BasicSyncExample()
    {
        using var synchronizer = new FileSynchronizer();

        var result = await synchronizer.SynchronizeAsync(
            originPath: @"C:\Source\Documents",
            destinationPath: @"C:\Backup\Documents", 
            regexPatterns: @".*\.(txt|doc|pdf)"
        );

        Console.WriteLine($"Sync completed: {result}");
    }

    /// <summary>
    /// File synchronization with progress reporting
    /// </summary>
    public static async Task ProgressReportingExample()
    {
        using var synchronizer = new FileSynchronizer(maxConcurrency: 4);

        var progress = new Progress<SyncProgress>(p =>
            Console.WriteLine($"Progress: {p.PercentComplete:F1}% - {p.CurrentOperation}"));

        var result = await synchronizer.SynchronizeAsync(
            originPath: @"C:\Source\Photos",
            destinationPath: @"C:\Backup\Photos",
            regexPatterns: @".*\.(jpg|jpeg|png|gif|bmp)",
            progress: progress
        );

        Console.WriteLine($"Photo sync completed: {result.FilesCreated} created, {result.FilesUpdated} updated");
    }

    /// <summary>
    /// File synchronization with cancellation support
    /// </summary>
    public static async Task CancellationExample()
    {
        using var synchronizer = new FileSynchronizer();
        using var cts = new CancellationTokenSource();

        // Cancel after 5 minutes
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            var result = await synchronizer.SynchronizeAsync(
                originPath: @"C:\Source\LargeFiles",
                destinationPath: @"C:\Backup\LargeFiles",
                regexPatterns: @".*\.(zip|rar|7z|tar|gz)",
                cancellationToken: cts.Token
            );

            Console.WriteLine($"Large files sync completed: {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Synchronization was cancelled due to timeout.");
        }
    }

    /// <summary>
    /// Complex regex patterns example
    /// </summary>
    public static async Task ComplexPatternsExample()
    {
        using var synchronizer = new FileSynchronizer();

        // Multiple patterns: log files from today, config files, and database backups
        var patterns = string.Join(";", new[]
        {
            @".*\.log",                    // All log files
            @".*config\.(xml|json|ini)",   // Configuration files
            @"backup_\d{8}\.db",          // Database backups with date pattern
            @"(?i)readme.*\.(txt|md)"     // README files (case insensitive)
        });

        var result = await synchronizer.SynchronizeAsync(
            originPath: @"C:\Applications\MyApp",
            destinationPath: @"C:\Backup\MyApp",
            regexPatterns: patterns
        );

        Console.WriteLine($"Application backup completed: {result}");
        
        if (!result.IsSuccess)
        {
            Console.WriteLine("Errors occurred:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }
}
