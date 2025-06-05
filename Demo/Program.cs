using FileSyncLibrary;

namespace FileSyncLibrary.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("File Synchronization Library - Retry Demo");
        Console.WriteLine("=========================================");

        // Create demo directories
        var demoPath = Path.Combine(Path.GetTempPath(), "FileSyncDemo");
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

            // Create some test files
            await File.WriteAllTextAsync(Path.Combine(originPath, "document1.txt"), "Content of document 1");
            await File.WriteAllTextAsync(Path.Combine(originPath, "document2.txt"), "Content of document 2");
            await File.WriteAllTextAsync(Path.Combine(originPath, "readme.md"), "# README\nThis is a markdown file");

            Console.WriteLine($"Created test files in: {originPath}");
            Console.WriteLine($"Target directory: {destinationPath}");
            Console.WriteLine();

            // Demonstrate synchronization without retries
            Console.WriteLine("=== Synchronization without retries (maxRetries = 0) ===");
            using var synchronizer = new FileSynchronizer();
            
            var result = await synchronizer.SynchronizeAsync(
                originPath, 
                destinationPath, 
                @".*\.(txt|md)", // Match .txt and .md files
                maxRetries: 0,
                progress: new Progress<SyncProgress>(progress =>
                {
                    Console.WriteLine($"  Progress: {progress.ProcessedFiles}/{progress.TotalFiles} - {progress.CurrentOperation}");
                }));

            Console.WriteLine($"Result: {result}");
            Console.WriteLine();

            // Demonstrate synchronization with retries
            Console.WriteLine("=== Synchronization with retries (maxRetries = 3) ===");
            
            // Modify a file to trigger an update
            await File.WriteAllTextAsync(Path.Combine(originPath, "document1.txt"), "Updated content of document 1");
            
            var resultWithRetries = await synchronizer.SynchronizeAsync(
                originPath, 
                destinationPath, 
                @".*\.(txt|md)", // Match .txt and .md files
                maxRetries: 3,
                progress: new Progress<SyncProgress>(progress =>
                {
                    Console.WriteLine($"  Progress: {progress.ProcessedFiles}/{progress.TotalFiles} - {progress.CurrentOperation}");
                }));

            Console.WriteLine($"Result: {resultWithRetries}");
            Console.WriteLine();

            // Show final state
            Console.WriteLine("=== Final synchronization results ===");
            Console.WriteLine($"Files in destination:");
            foreach (var file in Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(destinationPath, file);
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"  {relativePath}: {content.Length} chars - \"{content[..Math.Min(50, content.Length)]}...\"");
            }

            Console.WriteLine();
            Console.WriteLine("Demo completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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
