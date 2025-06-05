namespace FileSyncLibrary;

/// <summary>
/// Represents the action taken on a file during synchronization.
/// </summary>
public enum SyncAction
{
    /// <summary>
    /// The file was skipped because it's already up to date.
    /// </summary>
    Skipped,

    /// <summary>
    /// The file was created in the destination.
    /// </summary>
    Created,

    /// <summary>
    /// The file was updated in the destination.
    /// </summary>
    Updated,

    /// <summary>
    /// The file operation failed due to an error.
    /// </summary>
    Failed
}

/// <summary>
/// Represents the result of processing a single file during synchronization.
/// </summary>
/// <param name="Action">The action taken on the file</param>
/// <param name="FilePath">The relative path of the file</param>
/// <param name="Error">The error that occurred during processing, if any</param>
/// <param name="AttemptsRequired">The number of attempts required to complete the operation</param>
internal record FileProcessResult
{
    public required SyncAction Action { get; init; }
    public required string FilePath { get; init; }
    public Exception? Error { get; init; }
    public int AttemptsRequired { get; init; } = 1;
}
