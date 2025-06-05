using FileSyncLibrary;

namespace Examples;

/// <summary>
/// Demonstrates the improved error handling capabilities of FileSyncLibrary
/// </summary>
public class ErrorHandlingDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("=== FileSyncLibrary Error Handling Demo ===\n");

        using var synchronizer = new FileSynchronizer();

        // Create temporary directories for demonstration
        var tempRoot = Path.Combine(Path.GetTempPath(), "FileSyncDemo");
        var sourcePath = Path.Combine(tempRoot, "source");
        var destPath = Path.Combine(tempRoot, "destination");

        try
        {
            // Set up demonstration scenario
            await SetupDemoScenario(sourcePath, destPath);

            Console.WriteLine("Starting synchronization with mixed success/failure scenario...\n");

            // Progress reporting to show detailed actions
            var progress = new Progress<SyncProgress>(p =>
                Console.WriteLine($"[{p.PercentComplete:F1}%] {p.CurrentOperation}"));

            // Perform synchronization
            var result = await synchronizer.SynchronizeAsync(
                sourcePath,
                destPath,
                @".*\.txt",
                progress);

            // Display comprehensive results
            Console.WriteLine("\n=== Synchronization Results ===");
            Console.WriteLine(result.ToString());
            Console.WriteLine($"Overall Success: {result.IsSuccess}");

            if (result.FilesFailed > 0)
            {
                Console.WriteLine("\n=== Error Details ===");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"❌ {error}");
                }
            }

            if (result.FilesModified > 0)
            {
                Console.WriteLine($"\n✅ Successfully processed {result.FilesModified} files despite any failures");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Console.WriteLine("\n=== Demo Complete ===");
    }

    private static async Task SetupDemoScenario(string sourcePath, string destPath)
    {
        Directory.CreateDirectory(sourcePath);
        Directory.CreateDirectory(destPath);

        // Create various test files
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "success1.txt"), "This file will be created successfully");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "success2.txt"), "This file will be updated successfully");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "locked.txt"), "This file will fail due to lock");

        // Set up destination file to be updated
        await File.WriteAllTextAsync(Path.Combine(destPath, "success2.txt"), "Old content");
        File.SetLastWriteTime(Path.Combine(destPath, "success2.txt"), DateTime.Now.AddDays(-1));

        // Create a locked file to simulate failure (this is simplified for demo)
        var lockedFile = Path.Combine(destPath, "locked.txt");
        await File.WriteAllTextAsync(lockedFile, "Old locked content");
        
        Console.WriteLine("Demo scenario set up:");
        Console.WriteLine("- success1.txt: Will be created (new file)");
        Console.WriteLine("- success2.txt: Will be updated (newer source)");
        Console.WriteLine("- locked.txt: May fail if file becomes locked");
        Console.WriteLine();
    }
}
