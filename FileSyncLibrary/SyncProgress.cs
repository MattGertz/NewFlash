namespace FileSyncLibrary;

/// <summary>
/// Represents progress information for file synchronization operations.
/// </summary>
/// <param name="ProcessedFiles">Number of files processed so far</param>
/// <param name="TotalFiles">Total number of files to process</param>
/// <param name="CurrentOperation">Description of the current operation</param>
public record SyncProgress(int ProcessedFiles, int TotalFiles, string CurrentOperation)
{
    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;

    /// <summary>
    /// Gets a formatted progress string.
    /// </summary>
    /// <returns>A formatted string showing progress information.</returns>
    public override string ToString()
    {
        return $"{ProcessedFiles}/{TotalFiles} ({PercentComplete:F1}%) - {CurrentOperation}";
    }
}
