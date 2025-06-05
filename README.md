# FileSyncLibrary

A high-performance, asynchronous C# library for synchronizing files between directories based on regular expression patterns.

## Features

- ✅ **Asynchronous Operations**: Non-blocking file operations using async/await
- ✅ **Regex Pattern Matching**: Filter files using semicolon-separated regular expressions
- ✅ **Thread-Safe**: Controlled concurrency with configurable limits
- ✅ **Progress Reporting**: Real-time progress updates during synchronization
- ✅ **Smart File Comparison**: Only copies files that are missing or outdated
- ✅ **Recursive Directory Processing**: Preserves directory structure
- ✅ **Comprehensive Error Handling**: Detailed error reporting and graceful failures
- ✅ **Cancellation Support**: Respects cancellation tokens for responsive operations

## Installation

Add the library to your project:

```bash
dotnet add reference path/to/FileSyncLibrary.csproj
```

## Quick Start

```csharp
using FileSyncLibrary;

// Create a synchronizer with default settings
using var synchronizer = new FileSynchronizer();

// Synchronize all .txt and .log files
var result = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup", 
    regexPatterns: @".*\.txt;.*\.log"
);

Console.WriteLine($"Sync completed: {result.FilesCreated} created, {result.FilesUpdated} updated");
```

## Advanced Usage

### With Progress Reporting

```csharp
var progress = new Progress<SyncProgress>(p => 
    Console.WriteLine($"Progress: {p.PercentComplete:F1}% - {p.CurrentOperation}"));

var result = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.(txt|log|doc)",
    progress: progress
);
```

### With Cancellation Support

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5)); // Cancel after 5 minutes

try
{
    var result = await synchronizer.SynchronizeAsync(
        originPath: @"C:\Source",
        destinationPath: @"C:\Backup",
        regexPatterns: @".*\.(txt|log)",
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Synchronization was cancelled");
}
```

### Custom Concurrency Control

```csharp
// Limit to 4 concurrent file operations
using var synchronizer = new FileSynchronizer(maxConcurrency: 4);
```

## API Reference

### FileSynchronizer Class

#### Constructor
```csharp
FileSynchronizer(int maxConcurrency = 0)
```
- `maxConcurrency`: Maximum concurrent file operations (defaults to `Environment.ProcessorCount`)

#### SynchronizeAsync Method
```csharp
Task<SyncResult> SynchronizeAsync(
    string originPath, 
    string destinationPath, 
    string regexPatterns,
    IProgress<SyncProgress>? progress = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `originPath`: Source directory path
- `destinationPath`: Destination directory path  
- `regexPatterns`: Semicolon-separated regex patterns for file matching
- `progress`: Optional progress reporter
- `cancellationToken`: Cancellation token

**Returns:** `SyncResult` containing operation statistics

### SyncResult Class

Properties:
- `TotalFiles`: Total number of files processed
- `FilesCreated`: Number of new files created
- `FilesUpdated`: Number of existing files updated
- `FilesSkipped`: Number of files skipped (already up-to-date)
- `FilesFailed`: Number of files that failed to process
- `FilesModified`: Total files created + updated
- `IsSuccess`: True if no files failed to process
- `Errors`: Collection of detailed error messages for failed files

### SyncProgress Record

Properties:
- `ProcessedFiles`: Number of files processed so far
- `TotalFiles`: Total number of files to process
- `CurrentOperation`: Description of current operation
- `PercentComplete`: Completion percentage (0-100)

## Regular Expression Examples

| Pattern | Description |
|---------|-------------|
| `.*\.txt` | All .txt files |
| `.*\.(jpg\|png\|gif)` | Image files |
| `^backup_.*` | Files starting with "backup_" |
| `.*_[0-9]{4}\.log` | Log files with 4-digit suffix |
| `(?i).*readme.*` | Files containing "readme" (case-insensitive) |

## Performance Considerations

- **Concurrency**: Default concurrency is set to `Environment.ProcessorCount` for optimal CPU utilization
- **Memory Usage**: Files are copied using buffered streams with 4KB buffers
- **I/O Efficiency**: All file operations use async I/O to avoid blocking threads
- **Large Files**: The library handles large files efficiently through streaming

## Error Handling

The library provides comprehensive error handling with graceful failure recovery:

### Individual File Failures
- **Resilient Processing**: If one file fails to copy, the operation continues with remaining files
- **Detailed Error Reporting**: Each failure is logged with the specific file path and error message
- **Action Tracking**: Failed operations are tracked separately in `SyncResult.FilesFailed`
- **Partial Success**: The operation can complete successfully even with individual file failures

### Error Recovery Example
```csharp
var result = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.txt"
);

if (!result.IsSuccess)
{
    Console.WriteLine($"Completed with {result.FilesFailed} failures:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  {error}");
    }
}

Console.WriteLine($"Successfully processed: {result.FilesCreated + result.FilesUpdated} files");
```

### Types of Errors Handled
- **Access Denied**: Read-only files, permission issues
- **File Locks**: Files in use by other processes  
- **I/O Errors**: Network interruptions, disk full
- **Path Issues**: Invalid characters, path too long
- **Directory Creation**: Permission issues creating destination folders

### Error Reporting
- **Input Validation**: Validates all parameters before processing
- **Directory Creation**: Automatically creates destination directories
- **File Conflicts**: Gracefully handles file access conflicts
- **Detailed Messages**: Each error includes the file path and specific error description

## Thread Safety

The `FileSynchronizer` class is thread-safe and can be used concurrently. However, for optimal performance, create one instance per synchronization operation.

## Building and Testing

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Requirements

- .NET 8.0 or later
- Windows, macOS, or Linux

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
