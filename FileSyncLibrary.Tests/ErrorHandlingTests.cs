using FileSyncLibrary;
using System.Collections.Concurrent;
using System.Text;

namespace FileSyncLibrary.Tests;

public class ErrorHandlingTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _originPath;
    private readonly string _destinationPath;
    private readonly FileSynchronizer _synchronizer;

    public ErrorHandlingTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _originPath = Path.Combine(_testRoot, "origin");
        _destinationPath = Path.Combine(_testRoot, "destination");
        _synchronizer = new FileSynchronizer();

        Directory.CreateDirectory(_originPath);
        Directory.CreateDirectory(_destinationPath);
    }

    [Fact]
    public async Task SynchronizeAsync_WithLockedDestinationFile_ShouldReportFailureAndContinueProcessing()
    {
        // Arrange
        var goodFile = Path.Combine(_originPath, "good.txt");
        var lockedFile = Path.Combine(_originPath, "locked.txt");
        
        await File.WriteAllTextAsync(goodFile, "Good content");
        await File.WriteAllTextAsync(lockedFile, "New content");
        
        // Create a locked file in destination by keeping it open
        var destinationLockedFile = Path.Combine(_destinationPath, "locked.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationLockedFile)!);
        
        using var lockedStream = File.Create(destinationLockedFile);
        await lockedStream.WriteAsync(Encoding.UTF8.GetBytes("Old content"));
        await lockedStream.FlushAsync();
        
        // Make source file newer to trigger update attempt
        File.SetLastWriteTime(lockedFile, DateTime.Now.AddMinutes(1));

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated); // good.txt should be created
        Assert.Equal(0, result.FilesUpdated); // locked.txt should fail to update
        Assert.Equal(0, result.FilesSkipped);
        Assert.Equal(1, result.FilesFailed); // locked.txt should fail
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("locked.txt", result.Errors.First());

        // Verify the good file was still processed successfully
        var goodDestination = Path.Combine(_destinationPath, "good.txt");
        Assert.True(File.Exists(goodDestination));
        Assert.Equal("Good content", await File.ReadAllTextAsync(goodDestination));
    }

    [Fact]
    public async Task SynchronizeAsync_WithMixedResultsIncludingFailures_ShouldReportCorrectStatistics()
    {
        // Arrange
        var createFile = Path.Combine(_originPath, "create.txt");
        var updateFile = Path.Combine(_originPath, "update.txt");
        var skipFile = Path.Combine(_originPath, "skip.txt");
        var failFile = Path.Combine(_originPath, "fail.txt");
        
        await File.WriteAllTextAsync(createFile, "Create content");
        await File.WriteAllTextAsync(updateFile, "Update content");
        await File.WriteAllTextAsync(skipFile, "Skip content");
        await File.WriteAllTextAsync(failFile, "Fail content");
        
        // Set up destination files
        var destUpdateFile = Path.Combine(_destinationPath, "update.txt");
        var destSkipFile = Path.Combine(_destinationPath, "skip.txt");
        var destFailFile = Path.Combine(_destinationPath, "fail.txt");
        
        await File.WriteAllTextAsync(destUpdateFile, "Old update content");
        await File.WriteAllTextAsync(destSkipFile, "Skip content");
        
        // Make update file older, skip file same time, and fail file locked
        File.SetLastWriteTime(destUpdateFile, DateTime.Now.AddDays(-1));
        File.SetLastWriteTime(destSkipFile, File.GetLastWriteTime(skipFile));
        
        using var lockedStream = File.Create(destFailFile);
        await lockedStream.WriteAsync(Encoding.UTF8.GetBytes("Old fail content"));
        await lockedStream.FlushAsync();
        File.SetLastWriteTime(failFile, DateTime.Now.AddMinutes(1));

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(4, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated); // create.txt
        Assert.Equal(1, result.FilesUpdated); // update.txt
        Assert.Equal(1, result.FilesSkipped); // skip.txt
        Assert.Equal(1, result.FilesFailed); // fail.txt
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        
        // Verify the successful operations
        Assert.True(File.Exists(Path.Combine(_destinationPath, "create.txt")));
        Assert.Equal("Update content", await File.ReadAllTextAsync(destUpdateFile));
        Assert.Equal("Skip content", await File.ReadAllTextAsync(destSkipFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithProgressReporting_ShouldReportFailedActions()
    {
        // Arrange
        var goodFile = Path.Combine(_originPath, "good.txt");
        var failFile = Path.Combine(_originPath, "fail.txt");
        
        await File.WriteAllTextAsync(goodFile, "Good content");
        await File.WriteAllTextAsync(failFile, "Fail content");
        
        var destFailFile = Path.Combine(_destinationPath, "fail.txt");
        using var lockedStream = File.Create(destFailFile);
        await lockedStream.WriteAsync(Encoding.UTF8.GetBytes("Old content"));
        await lockedStream.FlushAsync();
        File.SetLastWriteTime(failFile, DateTime.Now.AddMinutes(1));
        
        var progressReports = new ConcurrentBag<SyncProgress>();

        // Act
        var result = await _synchronizer.SynchronizeAsync(
            _originPath, 
            _destinationPath, 
            @".*\.txt",
            new Progress<SyncProgress>(p => progressReports.Add(p)));

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.Equal(1, result.FilesFailed);
        
        // Verify progress reports contain both success and failure actions
        var actionReports = progressReports.Where(p => p.CurrentOperation.Contains(":")).ToList();
        Assert.Contains(actionReports, r => r.CurrentOperation.StartsWith("Created:"));
        Assert.Contains(actionReports, r => r.CurrentOperation.StartsWith("Failed:"));
    }

    [Fact]
    public void SyncResult_WithFailures_ShouldReportCorrectStatus()
    {
        // Arrange
        var result = new SyncResult
        {
            TotalFiles = 5,
            FilesCreated = 2,
            FilesUpdated = 1,
            FilesSkipped = 1,
            FilesFailed = 1
        };
        result.Errors.Add("test.txt: Access denied");

        // Act & Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.FilesModified); // Created + Updated
        Assert.Single(result.Errors);
        
        var toString = result.ToString();
        Assert.Contains("1 failed", toString);
    }

    public void Dispose()
    {
        _synchronizer?.Dispose();
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, true);
            }
            catch
            {
                // Ignore cleanup errors - might have locked files
            }
        }
    }
}
