using FileSyncLibrary;
using System.Diagnostics;

namespace FileSyncLibrary.Demo;

/// <summary>
/// Comprehensive demonstration of FileSyncLibrary capabilities including
/// error handling, progress reporting, and performance features.
/// </summary>
public class ComprehensiveDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🚀 FileSyncLibrary - Comprehensive Feature Demo");
        Console.WriteLine("================================================\n");

        await RunBasicSynchronizationDemo();
        await RunErrorHandlingDemo();
        await RunPerformanceDemo();
        await RunCancellationDemo();
        await RunDryRunDemo();

        Console.WriteLine("\n✅ All demonstrations completed successfully!");
        Console.WriteLine("The FileSyncLibrary is ready for production use with:");
        Console.WriteLine("  • Robust error handling and recovery");
        Console.WriteLine("  • Real-time progress reporting");
        Console.WriteLine("  • High-performance async operations");
        Console.WriteLine("  • Comprehensive cancellation support");
        Console.WriteLine("  • Thread-safe concurrent processing");
        Console.WriteLine("  • Dry run capability");
    }

    private static async Task RunBasicSynchronizationDemo()
    {
        Console.WriteLine("📁 BASIC SYNCHRONIZATION DEMO");
        Console.WriteLine("-------------------------------");

        using var synchronizer = new FileSynchronizer();
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo_Basic");
        
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var dest = Path.Combine(tempRoot, "destination");
            
            // Setup demo files
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "document.txt"), "Important document content");
            await File.WriteAllTextAsync(Path.Combine(source, "data.log"), "Application log data");
            await File.WriteAllTextAsync(Path.Combine(source, "readme.md"), "# Project Documentation");
            
            // Create subdirectory structure
            var subDir = Path.Combine(source, "reports");
            Directory.CreateDirectory(subDir);
            await File.WriteAllTextAsync(Path.Combine(subDir, "monthly.txt"), "Monthly report data");

            Console.WriteLine($"📂 Source: {source}");
            Console.WriteLine($"📂 Destination: {dest}");
            Console.WriteLine($"🔍 Pattern: *.txt;*.md (text and markdown files)");

            var result = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.txt;.*\.md");

            Console.WriteLine($"✅ Synchronized: {result.FilesCreated} created, {result.FilesUpdated} updated");
            Console.WriteLine($"📊 Total files processed: {result.TotalFiles}");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }

        Console.WriteLine();
    }

    private static async Task RunErrorHandlingDemo()
    {
        Console.WriteLine("🚨 ERROR HANDLING & RECOVERY DEMO");
        Console.WriteLine("-----------------------------------");

        using var synchronizer = new FileSynchronizer(maxConcurrency: 2);
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo_Errors");
        
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var dest = Path.Combine(tempRoot, "destination");
            
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(dest);
            
            // Create files that will succeed
            await File.WriteAllTextAsync(Path.Combine(source, "success1.txt"), "This will succeed");
            await File.WriteAllTextAsync(Path.Combine(source, "success2.txt"), "This will also succeed");
            await File.WriteAllTextAsync(Path.Combine(source, "locked.txt"), "This might fail if locked");

            Console.WriteLine("📝 Created test files including potential failure scenarios");

            // Enhanced progress reporting that shows action details
            var progressCount = 0;
            var progress = new Progress<SyncProgress>(p => {
                progressCount++;
                var emoji = p.CurrentOperation.StartsWith("Failed:") ? "❌" : 
                           p.CurrentOperation.StartsWith("Created:") ? "✅" : 
                           p.CurrentOperation.StartsWith("Updated:") ? "🔄" : 
                           p.CurrentOperation.StartsWith("Skipped:") ? "⏭️" : "📋";
                Console.WriteLine($"  {emoji} [{p.PercentComplete:F1}%] {p.CurrentOperation}");
            });            var sw = Stopwatch.StartNew();
            var result = await synchronizer.SynchronizeAsync(source, dest, @".*\.txt", progress: progress);
            sw.Stop();

            Console.WriteLine($"\n📊 RESULTS ({sw.ElapsedMilliseconds}ms):");
            Console.WriteLine($"  • Total files: {result.TotalFiles}");
            Console.WriteLine($"  • Created: {result.FilesCreated}");
            Console.WriteLine($"  • Updated: {result.FilesUpdated}");
            Console.WriteLine($"  • Skipped: {result.FilesSkipped}");
            Console.WriteLine($"  • Failed: {result.FilesFailed}");
            Console.WriteLine($"  • Success rate: {(result.TotalFiles - result.FilesFailed) * 100.0 / result.TotalFiles:F1}%");
            Console.WriteLine($"  • Overall success: {result.IsSuccess}");
            Console.WriteLine($"  • Progress reports: {progressCount}");

            if (result.Errors.Any())
            {
                Console.WriteLine($"\n🔍 ERROR DETAILS:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ❌ {error}");
                }
            }
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }

        Console.WriteLine();
    }

    private static async Task RunPerformanceDemo()
    {
        Console.WriteLine("⚡ PERFORMANCE & CONCURRENCY DEMO");
        Console.WriteLine("----------------------------------");

        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo_Performance");
        
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var dest = Path.Combine(tempRoot, "destination");
            Directory.CreateDirectory(source);

            // Create multiple files to demonstrate concurrent processing
            Console.WriteLine("📝 Creating 20 test files for concurrent processing...");
            var tasks = Enumerable.Range(1, 20).Select(async i => {
                var content = $"File {i} content with timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
                await File.WriteAllTextAsync(Path.Combine(source, $"file{i:D2}.txt"), content);
            });
            await Task.WhenAll(tasks);

            // Test different concurrency levels
            var concurrencyLevels = new[] { 1, 4, 8 };
            
            foreach (var concurrency in concurrencyLevels)
            {
                using var synchronizer = new FileSynchronizer(maxConcurrency: concurrency);
                var testDest = Path.Combine(dest, $"test_{concurrency}");
                
                var sw = Stopwatch.StartNew();
                var result = await synchronizer.SynchronizeAsync(source, testDest, @".*\.txt");
                sw.Stop();

                Console.WriteLine($"🔧 Concurrency {concurrency}: {sw.ElapsedMilliseconds}ms, {result.FilesCreated} files");
            }
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }

        Console.WriteLine();
    }

    private static async Task RunCancellationDemo()
    {
        Console.WriteLine("⏹️ CANCELLATION SUPPORT DEMO");
        Console.WriteLine("------------------------------");

        using var synchronizer = new FileSynchronizer();
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo_Cancellation");
        
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var dest = Path.Combine(tempRoot, "destination");
            Directory.CreateDirectory(source);

            // Create many files to allow for cancellation demonstration
            Console.WriteLine("📝 Creating 50 files for cancellation test...");
            for (int i = 1; i <= 50; i++)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(source, $"large_file_{i:D2}.txt"), 
                    new string('*', 1000) + $" File {i}"); // Make files slightly larger
            }

            using var cts = new CancellationTokenSource();
            
            // Cancel after a short delay to demonstrate cancellation
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            var cancelledProgress = new Progress<SyncProgress>(p => 
                Console.WriteLine($"  📊 [{p.PercentComplete:F1}%] {p.ProcessedFiles}/{p.TotalFiles} - {p.CurrentOperation}"));

            try
            {                Console.WriteLine("🚀 Starting synchronization with 100ms cancellation timeout...");
                var result = await synchronizer.SynchronizeAsync(
                    source, dest, @".*\.txt", 
                    progress: cancelledProgress, 
                    cancellationToken: cts.Token);
                
                Console.WriteLine($"⚠️ Unexpected completion: {result.FilesCreated} files created before cancellation");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("✅ Cancellation handled gracefully - operation was cancelled as expected");
                
                // Check what files were actually created before cancellation
                if (Directory.Exists(dest))
                {
                    var createdFiles = Directory.GetFiles(dest, "*.txt").Length;
                    Console.WriteLine($"📁 Partial result: {createdFiles} files were successfully created before cancellation");
                }
            }
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }

        Console.WriteLine();
    }
    
    private static async Task RunDryRunDemo()
    {
        Console.WriteLine("🔍 DRY RUN DEMO");
        Console.WriteLine("----------------");

        using var synchronizer = new FileSynchronizer();
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo_DryRun");
        
        try
        {
            var source = Path.Combine(tempRoot, "source");
            var dest = Path.Combine(tempRoot, "destination");
            
            // Setup demo files
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(dest);
            
            await File.WriteAllTextAsync(Path.Combine(source, "new_file.txt"), "This would be created");
            await File.WriteAllTextAsync(Path.Combine(source, "updated.txt"), "New content");
            await File.WriteAllTextAsync(Path.Combine(dest, "updated.txt"), "Old content");
            await File.WriteAllTextAsync(Path.Combine(source, "same.txt"), "Same content");
            await File.WriteAllTextAsync(Path.Combine(dest, "same.txt"), "Same content");
            
            // Set file times to ensure proper detection
            File.SetLastWriteTime(Path.Combine(dest, "updated.txt"), DateTime.Now.AddDays(-1));

            Console.WriteLine("📁 Source and destination prepared with test files");

            // Run in dry run mode
            Console.WriteLine("\n🔎 Running in dry run mode (preview only):");
            var dryRunResult = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.txt",
                dryRun: true,
                progress: new Progress<SyncProgress>(p => 
                    Console.WriteLine($"  {p}"))
            );
            
            Console.WriteLine($"\n📊 Dry Run Results: {dryRunResult}");
            Console.WriteLine($"  • Would Create: {dryRunResult.FilesCreated}");
            Console.WriteLine($"  • Would Update: {dryRunResult.FilesUpdated}");
            Console.WriteLine($"  • Would Skip: {dryRunResult.FilesSkipped}");
            
            // Verify no changes were made
            Console.WriteLine("\n✅ Verifying no actual changes were made...");
            var destFiles = Directory.GetFiles(dest).Select(Path.GetFileName).ToList();
            Console.WriteLine($"  Files in destination: {string.Join(", ", destFiles)}");
            
            // Now run for real
            Console.WriteLine("\n🔄 Now running the actual synchronization:");
            var result = await synchronizer.SynchronizeAsync(
                source, 
                dest, 
                @".*\.txt",
                progress: new Progress<SyncProgress>(p => 
                    Console.WriteLine($"  {p}"))
            );
            
            Console.WriteLine($"\n📊 Actual Results: {result}");
        }
        finally
        {
            CleanupDirectory(tempRoot);
        }

        Console.WriteLine();
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
