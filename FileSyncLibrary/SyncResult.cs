using System.Collections.Concurrent;

namespace FileSyncLibrary;

/// <summary>
/// Represents the result of a file synchronization operation.
/// </summary>
public class SyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether this was a dry run operation.
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Gets or sets the total number of files processed.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files created.
    /// </summary>
    public int FilesCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of files updated.
    /// </summary>
    public int FilesUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of files skipped (already up to date).
    /// </summary>
    public int FilesSkipped { get; set; }

    /// <summary>
    /// Gets or sets the number of files that failed to process.
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// Gets or sets the total number of retry attempts made across all file operations.
    /// </summary>
    public int TotalRetryAttempts { get; set; }

    /// <summary>
    /// Gets the collection of errors that occurred during synchronization.
    /// </summary>
    public ConcurrentBag<string> Errors { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the synchronization completed successfully.
    /// </summary>
    public bool IsSuccess => FilesFailed == 0;

    /// <summary>
    /// Gets the total number of files that were modified (created or updated).
    /// </summary>
    public int FilesModified => FilesCreated + FilesUpdated;    /// <summary>
    /// Gets a summary of the synchronization results.
    /// </summary>
    /// <returns>A formatted string describing the synchronization results.</returns>
    public override string ToString()
    {
        var dryRunPrefix = IsDryRun ? "[DRY RUN] " : "";
        var baseMessage = $"{dryRunPrefix}Sync completed: {TotalFiles} total, {FilesCreated} created, {FilesUpdated} updated, {FilesSkipped} skipped, {FilesFailed} failed";
        if (TotalRetryAttempts > 0)
        {
            baseMessage += $", {TotalRetryAttempts} retries";
        }
        return baseMessage;
    }
}
