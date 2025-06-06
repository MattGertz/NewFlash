using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FileSyncLibrary;

/// <summary>
/// Provides asynchronous file synchronization functionality between directories
/// based on regular expression patterns.
/// </summary>
public class FileSynchronizer : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrency;

    /// <summary>
    /// Initializes a new instance of the FileSynchronizer class.
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of concurrent file operations. Defaults to Environment.ProcessorCount.</param>
    public FileSynchronizer(int maxConcurrency = 0)
    {
        _maxConcurrency = maxConcurrency <= 0 ? Environment.ProcessorCount : maxConcurrency;
        _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    /// <summary>
    /// Synchronizes files from origin to destination directory based on regex patterns.
    /// </summary>
    /// <param name="originPath">Source directory path</param>
    /// <param name="destinationPath">Destination directory path</param>
    /// <param name="regexPatterns">Semicolon-separated regex patterns for file matching</param>
    /// <param name="maxRetries">Maximum number of retry attempts per file (0 = no retries, default: 0)</param>
    /// <param name="dryRun">When true, reports what would be done without performing actual file operations (default: false)</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Synchronization result containing statistics</returns>
    public async Task<SyncResult> SynchronizeAsync(
        string originPath,
        string destinationPath,
        string regexPatterns,
        short maxRetries = 0,
        bool dryRun = false,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(regexPatterns);

        if (!Directory.Exists(originPath))
            throw new DirectoryNotFoundException($"Origin directory not found: {originPath}");

        // Ensure destination directory exists
        Directory.CreateDirectory(destinationPath);

        var patterns = ParseRegexPatterns(regexPatterns); var result = new SyncResult { IsDryRun = dryRun };
        var processedFiles = new ConcurrentBag<string>();

        try
        {
            var originFiles = await GetMatchingFilesAsync(originPath, patterns, cancellationToken);
            result.TotalFiles = originFiles.Count;

            var operationMode = dryRun ? "[DRY RUN] " : "";
            progress?.Report(new SyncProgress(0, result.TotalFiles, $"{operationMode}Starting synchronization...")); var tasks = originFiles.Select(async filePath =>
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    var fileResult = await ProcessFileAsync(originPath, destinationPath, filePath, maxRetries, dryRun, cancellationToken);
                    UpdateResult(result, fileResult);
                    processedFiles.Add(filePath);

                    var actionDescription = GetActionDescription(fileResult, dryRun);
                    progress?.Report(new SyncProgress(
                        processedFiles.Count,
                        result.TotalFiles,
                        $"{actionDescription}: {Path.GetFileName(filePath)}"));
                }
                finally
                {
                    _semaphore.Release();
                }
            }); await Task.WhenAll(tasks);

            var completionMessage = dryRun ? "[DRY RUN] Synchronization analysis completed" : "Synchronization completed";
            progress?.Report(new SyncProgress(result.TotalFiles, result.TotalFiles, completionMessage));
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Synchronization failed: {ex.Message}");
            throw;
        }
    }

    private static List<Regex> ParseRegexPatterns(string regexPatterns)
    {
        return regexPatterns
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(pattern => new Regex(pattern.Trim(), RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();
    }

    private static async Task<List<string>> GetMatchingFilesAsync(
        string rootPath, 
        List<Regex> patterns, 
        CancellationToken cancellationToken)
    {
        var matchingFiles = new List<string>();
        
        await Task.Run(() =>
        {
            var allFiles = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
            
            foreach (var filePath in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileName = Path.GetFileName(filePath);
                if (patterns.Any(pattern => pattern.IsMatch(fileName)))
                {
                    matchingFiles.Add(filePath);
                }
            }
        }, cancellationToken);

        return matchingFiles;
    }

    private static async Task<FileProcessResult> ProcessFileAsync(
        string originPath,
        string destinationPath,
        string originFilePath,
        short maxRetries,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(originPath, originFilePath);
        var destinationFilePath = Path.Combine(destinationPath, relativePath);
        var destinationDir = Path.GetDirectoryName(destinationFilePath)!;

        Exception? lastException = null;
        int attemptCount = 0;

        while (attemptCount <= maxRetries)
        {
            attemptCount++;
            try
            {
                // Ensure destination directory exists (even in dry run mode for validation)
                if (!dryRun)
                {
                    Directory.CreateDirectory(destinationDir);
                }

                var originInfo = new FileInfo(originFilePath);
                var destinationInfo = new FileInfo(destinationFilePath);

                if (!destinationInfo.Exists)
                {
                    if (!dryRun)
                    {
                        await CopyFileAsync(originFilePath, destinationFilePath, cancellationToken);
                    }
                    return new FileProcessResult
                    {
                        Action = SyncAction.Created,
                        FilePath = relativePath,
                        AttemptsRequired = attemptCount
                    };
                }

                if (originInfo.LastWriteTime > destinationInfo.LastWriteTime)
                {
                    if (!dryRun)
                    {
                        await CopyFileAsync(originFilePath, destinationFilePath, cancellationToken);
                    }
                    return new FileProcessResult
                    {
                        Action = SyncAction.Updated,
                        FilePath = relativePath,
                        AttemptsRequired = attemptCount
                    };
                }

                return new FileProcessResult
                {
                    Action = SyncAction.Skipped,
                    FilePath = relativePath,
                    AttemptsRequired = attemptCount
                };
            }
            catch (Exception ex) when (attemptCount <= maxRetries)
            {
                lastException = ex;

                // Wait before retrying (exponential backoff)
                if (attemptCount <= maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attemptCount - 1) * 100);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        // All retries exhausted
        return new FileProcessResult
        {
            Action = SyncAction.Failed,
            FilePath = relativePath,
            Error = lastException,
            AttemptsRequired = attemptCount
        };
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        const int bufferSize = 4096;
        
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        
        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
    }

    private static void UpdateResult(SyncResult result, FileProcessResult fileResult)
    {
        lock (result)
        {
            // Track retry attempts (subtract 1 since AttemptsRequired includes the initial attempt)
            if (fileResult.AttemptsRequired > 1)
            {
                result.TotalRetryAttempts += (fileResult.AttemptsRequired - 1);
            }

            switch (fileResult.Action)
            {
                case SyncAction.Created:
                    result.FilesCreated++;
                    break;
                case SyncAction.Updated:
                    result.FilesUpdated++;
                    break;
                case SyncAction.Skipped:
                    result.FilesSkipped++;
                    break;
                case SyncAction.Failed:
                    result.FilesFailed++;
                    if (fileResult.Error != null)
                    {
                        result.Errors.Add($"{fileResult.FilePath}: {fileResult.Error.Message}");
                    }
                    break;
            }
        }
    }

    private static string GetActionDescription(FileProcessResult fileResult, bool dryRun = false)
    {
        return fileResult.Action switch
        {
            SyncAction.Created => dryRun ? "[DRY RUN] Would Create" : "Created",
            SyncAction.Updated => dryRun ? "[DRY RUN] Would Update" : "Updated",
            SyncAction.Skipped => dryRun ? "[DRY RUN] Would Skip" : "Skipped",
            SyncAction.Failed => "Failed",
            _ => dryRun ? "[DRY RUN] Would Process" : "Processed"
        };
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
