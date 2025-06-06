# FileSyncLibrary

A high-performance, asynchronous C# library for synchronizing files between directories based on regular expression patterns.

## 🤖 AI Development Experiment

**This project was developed entirely by AI using Claude 4.0 Sonnet in VS Code with GitHub Copilot.**

- **AI Model**: Claude 4.0 Sonnet (Anthropic)
- **Development Environment**: Visual Studio Code with GitHub Copilot extension
- **Code Generation**: 100% AI-generated code including all library code, unit tests, WPF client, and documentation
- **Human Role**: Requirements specification, testing feedback, and architectural guidance
- **Development Period**: Complete implementation with comprehensive testing and multiple client applications

## Features

- ✅ **Asynchronous Operations**: Non-blocking file operations using async/await
- ✅ **Regex Pattern Matching**: Filter files using semicolon-separated regular expressions
- ✅ **Thread-Safe**: Controlled concurrency with configurable limits
- ✅ **Progress Reporting**: Real-time progress updates during synchronization
- ✅ **Smart File Comparison**: Only copies files that are missing or outdated
- ✅ **Recursive Directory Processing**: Preserves directory structure
- ✅ **Comprehensive Error Handling**: Detailed error reporting and graceful failures
- ✅ **Retry Support**: Automatic retry with exponential backoff for transient failures
- ✅ **Cancellation Support**: Respects cancellation tokens for responsive operations
- ✅ **Dry Run Mode**: Preview synchronization operations without making file changes

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

### With Retry Support

```csharp
// Retry up to 3 times for transient failures (file locks, network issues)
var result = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.(txt|log)",
    maxRetries: 3
);

Console.WriteLine($"Files processed: {result.FilesModified}");
if (result.TotalRetryAttempts > 0)
{
    Console.WriteLine($"Total retry attempts made: {result.TotalRetryAttempts}");
}
```

### With Dry Run Mode

```csharp
// Preview what would happen without making any file changes
var dryRunResult = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.(txt|log)",
    dryRun: true
);

Console.WriteLine($"Dry run analysis: {dryRunResult}");
Console.WriteLine($"Would create {dryRunResult.FilesCreated} files");
Console.WriteLine($"Would update {dryRunResult.FilesUpdated} files");
Console.WriteLine($"Would skip {dryRunResult.FilesSkipped} files");

// Progress reporting shows dry run status
var progress = new Progress<SyncProgress>(p => 
    Console.WriteLine($"{p}"));  // Shows "[DRY RUN]" prefix in output

var detailedDryRun = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.(txt|log)",
    dryRun: true,
    progress: progress
);

// Output example:
// [DRY RUN] Would Create: document.txt
// [DRY RUN] Would Update: log.txt
// [DRY RUN] Would Skip: unchanged.txt
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
    short maxRetries = 0,
    bool dryRun = false,
    IProgress<SyncProgress>? progress = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `originPath`: Source directory path
- `destinationPath`: Destination directory path  
- `regexPatterns`: Semicolon-separated regex patterns for file matching
- `maxRetries`: Maximum retry attempts per file operation (0 = no retries)
- `dryRun`: When true, reports what would happen without making file changes
- `progress`: Optional progress reporter
- `cancellationToken`: Cancellation token

**Returns:** `SyncResult` containing operation statistics

### SyncResult Class

Properties:
- `TotalFiles`: Total number of files processed
- `FilesCreated`: Number of new files created (or would be created in dry run)
- `FilesUpdated`: Number of existing files updated (or would be updated in dry run)
- `FilesSkipped`: Number of files that failed to process
- `FilesModified`: Total files created + updated
- `TotalRetryAttempts`: Total number of retry attempts made across all files
- `IsSuccess`: True if no files failed to process
- `IsDryRun`: True if the results came from a dry run operation
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

## Dry Run Mode

The library provides a **dry run** capability that lets you preview synchronization operations without actually making any changes to the file system.

### Key Benefits

- **Preview Changes**: See exactly what files would be created, updated, or skipped
- **Safety**: Avoid unintended file operations before committing to them
- **Testing**: Validate regex patterns match the expected files
- **Reporting**: Generate reports on needed synchronization actions
- **Planning**: Estimate the scope of synchronization operations

### Dry Run Behavior

- No files are created or modified during dry run
- No directories are created during dry run
- File comparisons are still performed to determine required actions
- Progress messages are prefixed with `[DRY RUN]` 
- Actions are described as "Would Create", "Would Update", "Would Skip"
- `SyncResult` properties report what would have happened
- `SyncResult.ToString()` is prefixed with `[DRY RUN]`
- `SyncResult.IsDryRun` property is set to `true`

### Example Use Cases

1. **Validation Before Critical Operations**:
   ```csharp
   // First run in dry run mode to validate
   var dryRunResult = await synchronizer.SynchronizeAsync(sourcePath, backupPath, pattern, dryRun: true);
   
   // Check if the expected operations match intentions
   if (dryRunResult.FilesCreated > 100)
   {
       Console.WriteLine("Warning: This operation would create over 100 files!");
       Console.WriteLine("Please confirm before proceeding.");
       if (!UserConfirms())
           return;
   }
   
   // Now execute the actual synchronization
   var result = await synchronizer.SynchronizeAsync(sourcePath, backupPath, pattern);
   ```

2. **Reporting and Documentation**:
   ```csharp
   // Generate a report of pending changes
   var dryRunResult = await synchronizer.SynchronizeAsync(sourcePath, destPath, pattern, dryRun: true,
       progress: new Progress<SyncProgress>(p => LogOperation(p)));
   
   GenerateChangeReport(dryRunResult);
   ```

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
    regexPatterns: @".*\.txt",
    maxRetries: 3  // Retry up to 3 times for failed operations
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
if (result.TotalRetryAttempts > 0)
{
    Console.WriteLine($"Made {result.TotalRetryAttempts} retry attempts for transient failures");
}
```

### Retry Behavior
- **Exponential Backoff**: Delays between retries increase exponentially (100ms, 200ms, 400ms, etc.)
- **Transient Failure Recovery**: Automatically retries file locks, temporary I/O errors, and permission issues
- **Attempt Tracking**: Each file tracks the number of attempts required for statistics
- **Configurable Retries**: Set `maxRetries` parameter (0 = disabled, default)
- **Cancellation Aware**: Respects cancellation tokens during retry delays

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

## Development Issues and Lessons Learned

This section documents significant development challenges encountered during this AI-assisted development experiment, particularly with VS Code tooling integration.

### Critical VS Code Copilot Formatting Issues

**Problem**: VS Code Copilot's `replace_string_in_file` and `insert_edit_into_file` tools consistently produced syntax-breaking formatting corruption, specifically:

- **Missing Line Breaks**: Closing braces `}` concatenated with subsequent code on the same line
- **Region Statement Formatting**: `#region` and `#endregion` statements merged with method signatures
- **Method Definition Spacing**: Method definitions concatenated without proper line separation

**Impact**: 
- 7 critical formatting violations across 2 files required manual fixes
- Broken C# syntax that prevented compilation
- Repeated formatting corruption even after explicit correction instructions

**Root Cause Analysis**:
- Issue appears to be VS Code agent/tooling integration problem, not LLM comprehension issue
- Claude 4.0 Sonnet clearly understood formatting requirements from copilot-instructions.md
- Problem persisted despite explicit formatting rules and repeated corrections
- Suggests VS Code extension's file editing tools may have internal formatting bugs

**Workaround Required**:
- Manual verification and correction of every file edit
- Explicit formatting validation after each tool operation
- Conservative editing approach with smaller, focused changes

### VS Code Autosave Integration Problems

**Problem**: VS Code's autosave feature created race conditions with build operations:

- Build commands executed before file changes were written to disk
- Inconsistent file state between editor and file system
- Required manual "ask before building" workflow to ensure file persistence

**Impact**:
- Build failures with outdated file content
- Confusion between displayed code and actual file state
- Interrupted development workflow requiring manual verification

**Workaround**:
- Explicit save verification before any build operations
- Manual coordination between file editing and build processes
- Always confirm file persistence before executing build commands

### WPF Application Debugging Challenges

**Problem**: Silent application startup failures with minimal diagnostic information:

- WPF applications fail silently without console output
- Missing resource references caused immediate crashes
- Standard debugging approaches ineffective for startup issues

**Solution Implemented**:
- Comprehensive exception handling in App.xaml.cs
- File-based logging system for WPF diagnostics
- Unhandled exception handlers for both application and dispatcher
- Try-catch blocks around critical startup operations

### Icon Resource Configuration Complexity

**Problem**: WPF icon configuration requires multiple synchronized settings:

- Project file ApplicationIcon property
- Resource inclusion with proper build action
- XAML pack URI format for icon references
- Programmatic icon setting with error handling

**Key Learning**: Icon setup is more complex than simple file inclusion and requires careful coordination of multiple configuration points.

### Development Environment Recommendations

Based on this experiment, future AI-assisted development should consider:

1. **Tool Verification**: Always verify file editing tool output for syntax corruption
2. **Incremental Validation**: Check compilation after each significant edit
3. **Explicit Save Management**: Coordinate autosave with build operations
4. **Comprehensive Logging**: Implement detailed logging for applications without console output
5. **Conservative Editing**: Use smaller, focused edits to minimize formatting corruption risk

### Bug Report Status

A comprehensive bug report has been prepared for VS Code Copilot regarding the file editing tool formatting issues, emphasizing the syntax-breaking nature of the problem and its impact on development productivity.

These issues highlight the importance of robust tooling integration and the need for careful validation when using AI-assisted development tools in complex IDE environments.
