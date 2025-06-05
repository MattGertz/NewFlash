# FileSyncLibrary - Quick Reference Guide

## 🚀 Getting Started

### Basic Usage
```csharp
using FileSyncLibrary;

using var synchronizer = new FileSynchronizer();
var result = await synchronizer.SynchronizeAsync(
    originPath: @"C:\Source",
    destinationPath: @"C:\Backup",
    regexPatterns: @".*\.txt;.*\.log"
);
```

## 📋 API Quick Reference

### Constructor Options
```csharp
// Default concurrency (Environment.ProcessorCount)
var sync1 = new FileSynchronizer();

// Custom concurrency limit
var sync2 = new FileSynchronizer(maxConcurrency: 4);
```

### Synchronization Method
```csharp
Task<SyncResult> SynchronizeAsync(
    string originPath,              // Source directory
    string destinationPath,         // Target directory  
    string regexPatterns,          // Semicolon-separated patterns
    short maxRetries = 0,          // Retry attempts per file (0 = disabled)
    IProgress<SyncProgress>? progress = null,
    CancellationToken cancellationToken = default
)
```

### Retry Examples
```csharp
// No retries (default)
var result1 = await sync.SynchronizeAsync(source, dest, "*.txt");

// Retry up to 3 times for transient failures
var result2 = await sync.SynchronizeAsync(source, dest, "*.txt", maxRetries: 3);

// With progress and retry tracking
var result3 = await sync.SynchronizeAsync(source, dest, "*.txt", maxRetries: 2, progress: progressReporter);
Console.WriteLine($"Retry attempts made: {result3.TotalRetryAttempts}");
```

## 📊 Result Analysis

### SyncResult Properties
```csharp
var result = await synchronizer.SynchronizeAsync(...);

// File counts
int total = result.TotalFiles;        // All processed files
int created = result.FilesCreated;    // New files created
int updated = result.FilesUpdated;    // Existing files updated  
int skipped = result.FilesSkipped;    // Up-to-date files skipped
int failed = result.FilesFailed;     // Files that failed to process

// Status
bool success = result.IsSuccess;     // True if no failures
int modified = result.FilesModified; // Created + Updated
int retries = result.TotalRetryAttempts; // Total retry attempts made
var errors = result.Errors;          // Detailed error messages
```

## 🔍 Common Regex Patterns

| Pattern | Matches |
|---------|---------|
| `.*\.txt` | All .txt files |
| `.*\.(jpg\|png\|gif)` | Image files |
| `^backup_.*` | Files starting with "backup_" |
| `.*_[0-9]{4}\.log` | Log files with 4-digit year |
| `(?i).*readme.*` | Files containing "readme" (case-insensitive) |
| `.*\.(cs\|vb\|fs)` | C#, VB.NET, F# source files |
| `.*\.(doc\|docx\|pdf)` | Document files |

## 📈 Progress Reporting

### Basic Progress
```csharp
var progress = new Progress<SyncProgress>(p => 
    Console.WriteLine($"{p.PercentComplete:F1}% - {p.CurrentOperation}")
);

await synchronizer.SynchronizeAsync(source, dest, patterns, progress);
```

### Detailed Progress with Actions
```csharp
var progress = new Progress<SyncProgress>(p => {
    var emoji = p.CurrentOperation.StartsWith("Created:") ? "✅" :
                p.CurrentOperation.StartsWith("Updated:") ? "🔄" :
                p.CurrentOperation.StartsWith("Skipped:") ? "⏭️" :
                p.CurrentOperation.StartsWith("Failed:") ? "❌" : "📋";
    
    Console.WriteLine($"{emoji} [{p.PercentComplete:F1}%] {p.CurrentOperation}");
});
```

## 🚫 Cancellation Support

### Simple Cancellation
```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5));

try 
{
    var result = await synchronizer.SynchronizeAsync(
        source, dest, patterns, cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

### Manual Cancellation
```csharp
var cts = new CancellationTokenSource();

// Cancel from another thread/task
Task.Run(async () => {
    await Task.Delay(10000);
    cts.Cancel(); // Cancel after 10 seconds
});
```

## 🚨 Error Handling Patterns

### Check for Failures
```csharp
var result = await synchronizer.SynchronizeAsync(...);

if (!result.IsSuccess)
{
    Console.WriteLine($"Operation completed with {result.FilesFailed} failures:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  ❌ {error}");
    }
}

// Process successful files even if some failed
Console.WriteLine($"Successfully processed: {result.FilesModified} files");
```

### Handle Specific Errors
```csharp
var result = await synchronizer.SynchronizeAsync(...);

foreach (var error in result.Errors)
{
    if (error.Contains("Access is denied"))
    {
        Console.WriteLine($"Permission error: {error}");
    }
    else if (error.Contains("being used by another process"))
    {
        Console.WriteLine($"File lock error: {error}");
    }
    else
    {
        Console.WriteLine($"Other error: {error}");
    }
}
```

## ⚡ Performance Tips

### Optimize Concurrency
```csharp
// For CPU-bound operations
var sync = new FileSynchronizer(Environment.ProcessorCount);

// For I/O-bound operations (network drives)
var sync = new FileSynchronizer(Environment.ProcessorCount * 2);

// For limited resources
var sync = new FileSynchronizer(2);
```

### Efficient Patterns
```csharp
// ✅ Good: Specific patterns
var patterns = @".*\.txt;.*\.log";

// ❌ Avoid: Overly broad patterns that match many unwanted files
var patterns = @".*"; // This matches everything!
```

## 🔧 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| "Access is denied" | Check file permissions and ensure files aren't read-only |
| "File is being used" | Close applications that might have files open |
| "Path too long" | Use shorter paths or enable long path support |
| High memory usage | Reduce concurrency limit |
| Slow performance | Increase concurrency or check disk speed |

### Debug Information
```csharp
var result = await synchronizer.SynchronizeAsync(...);

Console.WriteLine($"Performance: {result.TotalFiles} files in {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Throughput: {result.TotalFiles * 1000.0 / sw.ElapsedMilliseconds:F1} files/second");
```

## 🏗️ Best Practices

1. **Always use `using` statements** for proper disposal
2. **Handle cancellation** in long-running operations  
3. **Monitor progress** for user feedback
4. **Check `IsSuccess`** before assuming all files synced
5. **Use specific regex patterns** to avoid unnecessary processing
6. **Test patterns** with a small subset first
7. **Consider concurrency limits** based on target storage type

## 📦 Dependencies

- **.NET 8.0** or later
- **System.Threading.Tasks** (built-in)
- **System.Text.RegularExpressions** (built-in)
- **System.Collections.Concurrent** (built-in)

---

*This library provides production-ready file synchronization with robust error handling, progress reporting, and high-performance async operations.*
