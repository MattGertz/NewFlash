using FileSyncLibrary;

namespace FileSyncLibrary.Demo;

/// <summary>
/// Provides various examples of using FileSyncLibrary
/// </summary>
public static class Examples
{
    /// <summary>
    /// Runs all example demos
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== FileSyncLibrary Usage Examples ===\n");
        
        await BasicSyncExample();
        await ProgressReportingExample();  
        await RetryExample();
        await ComplexPatternsExample();
        
        Console.WriteLine("\n=== Examples Complete ===");
    }

    /// <summary>
    /// Basic file synchronization example
    /// </summary>
    public static async Task BasicSyncExample()
    {
        Console.WriteLine("\n📄 BASIC SYNCHRONIZATION EXAMPLE");
        Console.WriteLine("----------------------------------");
        
        using var synchronizer = new FileSynchronizer();
        
        // Create demo directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncExample_Basic");
        var source = Path.Combine(tempRoot, "source");
        var dest = Path.Combine(tempRoot, "backup");
        
        try
        {
            // Create test files
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "document.txt"), "Important document content");
            await File.WriteAllTextAsync(Path.Combine(source, "report.pdf"), "PDF content simulation");
            
            Console.WriteLine($"Source: {source}");
            Console.WriteLine($"Destination: {dest}");
            Console.WriteLine($"Pattern: *.txt;*.doc;*.pdf");

            var result = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.(txt|doc|pdf)"
            );

            Console.WriteLine($"Sync completed: {result}");
        }
        finally
        {
            // Cleanup
            CleanupDirectory(tempRoot);
        }
    }

    /// <summary>
    /// File synchronization with progress reporting
    /// </summary>
    public static async Task ProgressReportingExample()
    {
        Console.WriteLine("\n📊 PROGRESS REPORTING EXAMPLE");
        Console.WriteLine("----------------------------------");
        
        using var synchronizer = new FileSynchronizer(maxConcurrency: 4);
        
        // Create demo directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncExample_Progress");
        var source = Path.Combine(tempRoot, "source");
        var dest = Path.Combine(tempRoot, "backup");
        
        try
        {
            // Create test files
            Directory.CreateDirectory(source);
            for (int i = 1; i <= 5; i++)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(source, $"photo{i}.jpg"), 
                    $"Image content {i}"
                );
            }
            
            Console.WriteLine($"Source: {source} (5 sample image files)");
            Console.WriteLine($"Destination: {dest}");
            Console.WriteLine($"Pattern: *.jpg");
            Console.WriteLine($"With progress reporting:");

            var progress = new Progress<SyncProgress>(p =>
                Console.WriteLine($"  Progress: {p.PercentComplete:F1}% - {p.CurrentOperation}"));

            var result = await synchronizer.SynchronizeAsync(
                source,
                dest,
                @".*\.jpg",
                progress: progress
            );

            Console.WriteLine($"Photo sync completed: {result.FilesCreated} created, {result.FilesUpdated} updated");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }
    }

    /// <summary>
    /// File synchronization with retry support for transient failures
    /// </summary>
    public static async Task RetryExample()
    {
        Console.WriteLine("\n🔄 RETRY SUPPORT EXAMPLE");
        Console.WriteLine("----------------------------------");
        
        using var synchronizer = new FileSynchronizer();
        
        // Create demo directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncExample_Retry");
        var source = Path.Combine(tempRoot, "source");
        var dest = Path.Combine(tempRoot, "backup");
        
        try
        {
            // Create test files
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "document1.txt"), "Document 1 content");
            await File.WriteAllTextAsync(Path.Combine(source, "document2.txt"), "Document 2 content");
            
            Console.WriteLine($"Source: {source} (sample documents)");
            Console.WriteLine($"Destination: {dest}");
            Console.WriteLine($"Pattern: *.txt");
            Console.WriteLine($"With retry support (max 3 attempts):");

            var progress = new Progress<SyncProgress>(p =>
            {
                var emoji = p.CurrentOperation.StartsWith("Failed:") ? "❌" : 
                           p.CurrentOperation.StartsWith("Retrying:") ? "🔄" : "✅";
                Console.WriteLine($"  {emoji} {p.CurrentOperation}");
            });

            var result = await synchronizer.SynchronizeAsync(
                source,
                dest,
                @".*\.txt",
                maxRetries: 3,
                progress: progress
            );

            Console.WriteLine($"Sync completed: {result}");
            Console.WriteLine($"Retry attempts: {result.TotalRetryAttempts}");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }
    }

    /// <summary>
    /// Complex regex patterns example
    /// </summary>
    public static async Task ComplexPatternsExample()
    {
        Console.WriteLine("\n🔍 COMPLEX PATTERNS EXAMPLE");
        Console.WriteLine("----------------------------------");
        
        using var synchronizer = new FileSynchronizer();
        
        // Create demo directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncExample_Patterns");
        var source = Path.Combine(tempRoot, "source");
        var dest = Path.Combine(tempRoot, "backup");
        
        try
        {
            // Create test files with varied patterns
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "app.log"), "Log file content");
            await File.WriteAllTextAsync(Path.Combine(source, "settings.config.xml"), "XML Config content");
            await File.WriteAllTextAsync(Path.Combine(source, "backup_20250605.db"), "Database backup");
            await File.WriteAllTextAsync(Path.Combine(source, "README.md"), "Project documentation");
            await File.WriteAllTextAsync(Path.Combine(source, "test.bin"), "Binary file - should not sync");
            
            Console.WriteLine($"Source: {source} (various file types)");
            Console.WriteLine($"Destination: {dest}");
            
            // Multiple patterns: log files, config files, database backups, and READMEs
            var patterns = string.Join(";", new[]
            {
                @".*\.log",                    // All log files
                @".*config\.(xml|json|ini)",   // Configuration files
                @"backup_\d{8}\.db",          // Database backups with date pattern
                @"(?i)readme.*\.(txt|md)"     // README files (case insensitive)
            });
            
            Console.WriteLine($"Pattern: {patterns}");

            var result = await synchronizer.SynchronizeAsync(
                source,
                dest,
                patterns
            );

            Console.WriteLine($"Sync completed: {result}");
            Console.WriteLine($"Files matched by regex patterns: {result.FilesCreated}");
            Console.WriteLine($"Files skipped (not matching): {result.FilesSkipped}");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }
    }
    
    /// <summary>
    /// Dry run example
    /// </summary>
    public static async Task DryRunExample()
    {
        Console.WriteLine("\n🔍 DRY RUN EXAMPLE");
        Console.WriteLine("----------------------------------");
        
        using var synchronizer = new FileSynchronizer();
        
        // Create demo directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncExample_DryRun");
        var source = Path.Combine(tempRoot, "source");
        var dest = Path.Combine(tempRoot, "backup");
        
        try
        {
            // Create test files
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(dest);
            
            await File.WriteAllTextAsync(Path.Combine(source, "new.txt"), "New file content");
            await File.WriteAllTextAsync(Path.Combine(source, "updated.txt"), "Updated content");
            await File.WriteAllTextAsync(Path.Combine(dest, "updated.txt"), "Old content");
            
            // Set file time to ensure update is detected
            File.SetLastWriteTime(Path.Combine(dest, "updated.txt"), DateTime.Now.AddDays(-1));
            
            Console.WriteLine($"Source: {source}");
            Console.WriteLine($"Destination: {dest}");
            Console.WriteLine($"Pattern: *.txt");
            Console.WriteLine($"In dry run mode (no actual changes):");
            
            // Perform dry run
            var dryRunResult = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.txt", 
                dryRun: true,
                progress: new Progress<SyncProgress>(p => 
                    Console.WriteLine($"  {p}"))
            );

            Console.WriteLine($"\nDry run result: {dryRunResult}");
            Console.WriteLine($"Would create: {dryRunResult.FilesCreated}");
            Console.WriteLine($"Would update: {dryRunResult.FilesUpdated}");
            
            // Verify no changes made
            bool newFileExists = File.Exists(Path.Combine(dest, "new.txt"));
            Console.WriteLine($"New file was actually created: {newFileExists} (should be False)");
            
            // Now do the actual sync
            Console.WriteLine("\nPerforming actual synchronization:");
            var result = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.txt"
            );
            
            Console.WriteLine($"Actual sync result: {result}");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }
    }

    private static void CleanupDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
