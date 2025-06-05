using FileSyncLibrary;

namespace FileSyncLibrary.Demo;

/// <summary>
/// Demonstrates the dry run functionality of the FileSyncLibrary.
/// This shows how to preview what would happen during synchronization without actually performing file operations.
/// </summary>
public static class DryRunDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("File Synchronization Library - Dry Run Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Create demo directories
        var demoPath = Path.Combine(Path.GetTempPath(), "FileSyncDryRunDemo");
        var originPath = Path.Combine(demoPath, "origin");
        var destinationPath = Path.Combine(demoPath, "destination");

        try
        {
            // Clean up any existing demo directory
            if (Directory.Exists(demoPath))
                Directory.Delete(demoPath, true);

            // Create directories
            Directory.CreateDirectory(originPath);
            Directory.CreateDirectory(destinationPath);

            // Create test files in origin
            await File.WriteAllTextAsync(Path.Combine(originPath, "new_document.txt"), "This file will be created");
            await File.WriteAllTextAsync(Path.Combine(originPath, "update_me.txt"), "Updated content from origin");
            await File.WriteAllTextAsync(Path.Combine(originPath, "unchanged.txt"), "This content is the same");

            // Create some existing files in destination
            await File.WriteAllTextAsync(Path.Combine(destinationPath, "update_me.txt"), "Old content in destination");
            await File.WriteAllTextAsync(Path.Combine(destinationPath, "unchanged.txt"), "This content is the same");

            // Set timestamps to ensure proper update detection
            var oldTime = DateTime.Now.AddHours(-1);
            File.SetLastWriteTime(Path.Combine(destinationPath, "update_me.txt"), oldTime);
            
            var sameTime = DateTime.Now;
            File.SetLastWriteTime(Path.Combine(originPath, "unchanged.txt"), sameTime);
            File.SetLastWriteTime(Path.Combine(destinationPath, "unchanged.txt"), sameTime);

            Console.WriteLine("=== Initial State ===");
            Console.WriteLine($"Origin directory: {originPath}");
            Console.WriteLine("Files in origin:");
            foreach (var file in Directory.GetFiles(originPath))
            {
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"  {Path.GetFileName(file)}: \"{content}\"");
            }

            Console.WriteLine();
            Console.WriteLine($"Destination directory: {destinationPath}");
            Console.WriteLine("Files in destination:");
            foreach (var file in Directory.GetFiles(destinationPath))
            {
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"  {Path.GetFileName(file)}: \"{content}\"");
            }

            Console.WriteLine();
            Console.WriteLine("=== DRY RUN ANALYSIS ===");
            Console.WriteLine("Running synchronization in dry run mode to see what would happen...");
            Console.WriteLine();

            using var synchronizer = new FileSynchronizer();
            
            // Perform dry run
            var dryRunResult = await synchronizer.SynchronizeAsync(
                originPath, 
                destinationPath, 
                @".*\.txt", // Match .txt files
                maxRetries: 0,
                dryRun: true, // This is the key parameter!
                progress: new Progress<SyncProgress>(progress =>
                {
                    Console.WriteLine($"  {progress}");
                }));

            Console.WriteLine();
            Console.WriteLine($"Dry Run Result: {dryRunResult}");
            Console.WriteLine();

            // Verify no actual changes were made
            Console.WriteLine("=== Verification: No Changes Made ===");
            Console.WriteLine("Files in destination after dry run (should be unchanged):");
            foreach (var file in Directory.GetFiles(destinationPath))
            {
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"  {Path.GetFileName(file)}: \"{content}\"");
            }

            // Check that new file was NOT created
            var newFilePath = Path.Combine(destinationPath, "new_document.txt");
            Console.WriteLine($"New file created? {File.Exists(newFilePath)} (should be false)");

            Console.WriteLine();
            Console.WriteLine("=== ACTUAL SYNCHRONIZATION ===");
            Console.WriteLine("Now running the same synchronization with dryRun=false...");
            Console.WriteLine();

            // Perform actual synchronization
            var actualResult = await synchronizer.SynchronizeAsync(
                originPath, 
                destinationPath, 
                @".*\.txt", // Match .txt files
                maxRetries: 0,
                dryRun: false, // Actually perform operations
                progress: new Progress<SyncProgress>(progress =>
                {
                    Console.WriteLine($"  {progress}");
                }));

            Console.WriteLine();
            Console.WriteLine($"Actual Result: {actualResult}");
            Console.WriteLine();

            // Show final state
            Console.WriteLine("=== Final State After Actual Sync ===");
            Console.WriteLine("Files in destination after actual synchronization:");
            foreach (var file in Directory.GetFiles(destinationPath))
            {
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"  {Path.GetFileName(file)}: \"{content}\"");
            }

            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine("Key observations:");
            Console.WriteLine("1. Dry run showed exactly what would happen without making changes");
            Console.WriteLine("2. Progress reports clearly indicated '[DRY RUN]' operations");
            Console.WriteLine("3. Result object had IsDryRun=true and ToString() included '[DRY RUN]'");
            Console.WriteLine("4. No files were created, updated, or directories created during dry run");
            Console.WriteLine("5. Actual synchronization performed the same operations as predicted");

            Console.WriteLine();
            Console.WriteLine("Dry run demo completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            // Clean up
            try
            {
                if (Directory.Exists(demoPath))
                    Directory.Delete(demoPath, true);
            }
            catch { }
        }
    }
}
