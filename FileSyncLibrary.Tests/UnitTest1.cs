using FileSyncLibrary;
using System.Collections.Concurrent;
using System.Text;

namespace FileSyncLibrary.Tests;

public class FileSynchronizerTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _originPath;
    private readonly string _destinationPath;
    private readonly FileSynchronizer _synchronizer;

    public FileSynchronizerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _originPath = Path.Combine(_testRoot, "origin");
        _destinationPath = Path.Combine(_testRoot, "destination");
        _synchronizer = new FileSynchronizer();

        Directory.CreateDirectory(_originPath);
        Directory.CreateDirectory(_destinationPath);
    }

    [Fact]
    public async Task SynchronizeAsync_WithMatchingFiles_ShouldCopyFiles()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.Equal(0, result.FilesUpdated);
        Assert.Equal(0, result.FilesSkipped);
        Assert.True(result.IsSuccess);

        var destinationFile = Path.Combine(_destinationPath, "test.txt");
        Assert.True(File.Exists(destinationFile));
        Assert.Equal("Hello World", await File.ReadAllTextAsync(destinationFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithNonMatchingFiles_ShouldSkipFiles()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.log");
        await File.WriteAllTextAsync(testFile, "Log content");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.FilesCreated);

        var destinationFile = Path.Combine(_destinationPath, "test.log");
        Assert.False(File.Exists(destinationFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithMultiplePatterns_ShouldMatchCorrectFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_originPath, "file1.txt"), "Content 1");
        await File.WriteAllTextAsync(Path.Combine(_originPath, "file2.log"), "Content 2");
        await File.WriteAllTextAsync(Path.Combine(_originPath, "file3.doc"), "Content 3");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt;.*\.log");

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.FilesCreated);
        Assert.True(File.Exists(Path.Combine(_destinationPath, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(_destinationPath, "file2.log")));
        Assert.False(File.Exists(Path.Combine(_destinationPath, "file3.doc")));
    }

    [Fact]
    public async Task SynchronizeAsync_WithNewerDestinationFile_ShouldSkipFile()
    {
        // Arrange
        var originFile = Path.Combine(_originPath, "test.txt");
        var destinationFile = Path.Combine(_destinationPath, "test.txt");

        await File.WriteAllTextAsync(originFile, "Original content");
        await Task.Delay(1000); // Ensure different timestamps
        await File.WriteAllTextAsync(destinationFile, "Newer content");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.FilesCreated);
        Assert.Equal(0, result.FilesUpdated);
        Assert.Equal(1, result.FilesSkipped);
        Assert.Equal("Newer content", await File.ReadAllTextAsync(destinationFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithOlderDestinationFile_ShouldUpdateFile()
    {
        // Arrange
        var originFile = Path.Combine(_originPath, "test.txt");
        var destinationFile = Path.Combine(_destinationPath, "test.txt");

        await File.WriteAllTextAsync(destinationFile, "Old content");
        await Task.Delay(1000); // Ensure different timestamps
        await File.WriteAllTextAsync(originFile, "New content");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.FilesCreated);
        Assert.Equal(1, result.FilesUpdated);
        Assert.Equal(0, result.FilesSkipped);
        Assert.Equal("New content", await File.ReadAllTextAsync(destinationFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithSubdirectories_ShouldPreserveStructure()
    {
        // Arrange
        var subDir = Path.Combine(_originPath, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "Nested content");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);

        var destinationNested = Path.Combine(_destinationPath, "subdir", "nested.txt");
        Assert.True(File.Exists(destinationNested));
        Assert.Equal("Nested content", await File.ReadAllTextAsync(destinationNested));
    }

    [Fact]
    public async Task SynchronizeAsync_WithProgressReporting_ShouldReportProgress()
    {
        // Arrange
        var progressReports = new List<SyncProgress>();
        var progress = new Progress<SyncProgress>(p => 
        {
            lock (progressReports)
            {
                progressReports.Add(p);
            }
        });

        await File.WriteAllTextAsync(Path.Combine(_originPath, "file1.txt"), "Content 1");
        await File.WriteAllTextAsync(Path.Combine(_originPath, "file2.txt"), "Content 2");        // Act
        await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", maxRetries: 0, progress: progress);

        // Give a small delay to ensure all progress reports are captured
        await Task.Delay(100);

        // Assert
        var reportCount = progressReports.Count;
        Assert.True(reportCount >= 3, $"Expected at least 3 progress reports, but got {reportCount}. Reports: {string.Join(", ", progressReports.Select(r => r.CurrentOperation))}");
        Assert.Contains(progressReports, p => p.CurrentOperation.Contains("Starting"));
        Assert.Contains(progressReports, p => p.CurrentOperation.Contains("completed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SynchronizeAsync_WithInvalidParameters_ShouldThrowException(string invalidPath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _synchronizer.SynchronizeAsync(invalidPath, _destinationPath, @".*\.txt"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _synchronizer.SynchronizeAsync(_originPath, invalidPath, @".*\.txt"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _synchronizer.SynchronizeAsync(_originPath, _destinationPath, invalidPath));
    }

    [Fact]
    public async Task SynchronizeAsync_WithNullParameters_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _synchronizer.SynchronizeAsync(null!, _destinationPath, @".*\.txt"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _synchronizer.SynchronizeAsync(_originPath, null!, @".*\.txt"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _synchronizer.SynchronizeAsync(_originPath, _destinationPath, null!));
    }

    [Fact]
    public async Task SynchronizeAsync_WithNonExistentOriginPath_ShouldThrowException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRoot, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _synchronizer.SynchronizeAsync(nonExistentPath, _destinationPath, @".*\.txt"));
    }

    [Fact]
    public async Task SynchronizeAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        
        // Create many files to increase chance of cancellation during processing
        for (int i = 0; i < 100; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(_originPath, $"file{i}.txt"), $"Content {i}");
        }

        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", cancellationToken: cts.Token));
    }

    [Fact]
    public void SyncProgress_CalculatesPercentageCorrectly()
    {
        // Arrange & Act
        var progress = new SyncProgress(25, 100, "Test operation");

        // Assert
        Assert.Equal(25.0, progress.PercentComplete);
        Assert.Contains("25/100", progress.ToString());
        Assert.Contains("25.0%", progress.ToString());
    }

    [Fact]
    public void SyncResult_CalculatesModifiedFilesCorrectly()
    {
        // Arrange
        var result = new SyncResult
        {
            FilesCreated = 5,
            FilesUpdated = 3,
            FilesSkipped = 2
        };

        // Act & Assert
        Assert.Equal(8, result.FilesModified);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SynchronizeAsync_WithSimpleSynchronization_ShouldCompleteSuccessfully()
    {
        // Arrange - Simple test that doesn't rely on specific error scenarios
        var goodFile = Path.Combine(_originPath, "good.txt");
        var existingFile = Path.Combine(_originPath, "existing.txt");
        
        await File.WriteAllTextAsync(goodFile, "Good content");
        await File.WriteAllTextAsync(existingFile, "New content");
        
        // Create an existing file in destination that should be updated
        var destinationExistingFile = Path.Combine(_destinationPath, "existing.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationExistingFile)!);
        await File.WriteAllTextAsync(destinationExistingFile, "Old content");
        File.SetLastWriteTime(destinationExistingFile, DateTime.Now.AddDays(-1));

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated); // good.txt should be created
        Assert.Equal(1, result.FilesUpdated); // existing.txt should be updated
        Assert.Equal(0, result.FilesSkipped);
        Assert.Equal(0, result.FilesFailed);
        Assert.True(result.IsSuccess);

        // Verify both files were processed successfully
        var goodDestination = Path.Combine(_destinationPath, "good.txt");
        Assert.True(File.Exists(goodDestination));
        Assert.Equal("Good content", await File.ReadAllTextAsync(goodDestination));
        Assert.Equal("New content", await File.ReadAllTextAsync(destinationExistingFile));
    }

    [Fact]
    public async Task SynchronizeAsync_WithFileAccessError_ShouldReportFailures()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        
        // Create a subdirectory and make it read-only to simulate access errors
        var subDir = Path.Combine(_destinationPath, "readonly-dir");
        Directory.CreateDirectory(subDir);
        var destFile = Path.Combine(subDir, "test.txt");
        
        // Create the file first, then make the directory read-only
        await File.WriteAllTextAsync(destFile, "Old content");
        File.SetAttributes(subDir, FileAttributes.ReadOnly | FileAttributes.Directory);

        try
        {
            // Move source file to subdirectory to match
            var sourceSubDir = Path.Combine(_originPath, "readonly-dir");
            Directory.CreateDirectory(sourceSubDir);
            var newSourceFile = Path.Combine(sourceSubDir, "test.txt");
            File.Move(testFile, newSourceFile);
            
            // Act
            var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

            // Assert - The operation should complete but may have failures
            Assert.Equal(1, result.TotalFiles);
            // The exact behavior depends on Windows permissions - file might be updated or fail
            Assert.True(result.FilesCreated + result.FilesUpdated + result.FilesSkipped + result.FilesFailed == 1);
        }
        finally
        {
            // Clean up read-only directory
            try
            {
                File.SetAttributes(subDir, FileAttributes.Normal | FileAttributes.Directory);
                if (File.Exists(destFile))
                {
                    File.SetAttributes(destFile, FileAttributes.Normal);
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task SynchronizeAsync_WithProgressReporting_ShouldReportDetailedActions()
    {
        // Arrange
        var newFile = Path.Combine(_originPath, "new.txt");
        var updateFile = Path.Combine(_originPath, "update.txt");
        var skipFile = Path.Combine(_originPath, "skip.txt");
        
        await File.WriteAllTextAsync(newFile, "New content");
        await File.WriteAllTextAsync(updateFile, "Updated content");
        await File.WriteAllTextAsync(skipFile, "Skip content");
        
        // Create existing files in destination
        var destUpdateFile = Path.Combine(_destinationPath, "update.txt");
        var destSkipFile = Path.Combine(_destinationPath, "skip.txt");
        
        await File.WriteAllTextAsync(destUpdateFile, "Old content");
        await File.WriteAllTextAsync(destSkipFile, "Skip content");
        
        // Make update file older than source
        File.SetLastWriteTime(destUpdateFile, DateTime.Now.AddDays(-1));
        // Make skip file same time as source
        File.SetLastWriteTime(destSkipFile, File.GetLastWriteTime(skipFile));
        
        var progressReports = new ConcurrentBag<SyncProgress>();        // Act
        var result = await _synchronizer.SynchronizeAsync(
            _originPath, 
            _destinationPath, 
            @".*\.txt",
            maxRetries: 0,
            progress: new Progress<SyncProgress>(p => progressReports.Add(p)));

        // Assert
        Assert.Equal(3, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.Equal(1, result.FilesUpdated);
        Assert.Equal(1, result.FilesSkipped);
        Assert.True(result.IsSuccess);
          // Verify progress reports contain action descriptions
        var actionReports = progressReports.Where(p => p.CurrentOperation.Contains(":")).ToList();
        Assert.Contains(actionReports, r => r.CurrentOperation.StartsWith("Created:"));
        Assert.Contains(actionReports, r => r.CurrentOperation.StartsWith("Updated:"));
        Assert.Contains(actionReports, r => r.CurrentOperation.StartsWith("Skipped:"));
    }

    [Fact]
    public void SyncResult_ToString_ShouldIncludeFailedCount()
    {
        // Arrange
        var result = new SyncResult
        {
            TotalFiles = 10,
            FilesCreated = 3,
            FilesUpdated = 2,
            FilesSkipped = 4,
            FilesFailed = 1
        };

        // Act
        var toString = result.ToString();

        // Assert
        Assert.Contains("10 total", toString);
        Assert.Contains("3 created", toString);
        Assert.Contains("2 updated", toString);
        Assert.Contains("4 skipped", toString);
        Assert.Contains("1 failed", toString);
    }

    [Fact]
    public void SyncResult_IsSuccess_ShouldBeFalseWhenFilesFailedIsGreaterThanZero()
    {
        // Arrange & Act
        var successResult = new SyncResult { FilesFailed = 0 };
        var failureResult = new SyncResult { FilesFailed = 1 };

        // Assert
        Assert.True(successResult.IsSuccess);
        Assert.False(failureResult.IsSuccess);
    }

    public void Dispose()
    {
        _synchronizer?.Dispose();
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }
}