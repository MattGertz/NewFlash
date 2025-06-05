using FileSyncLibrary;
using System.Collections.Concurrent;

namespace FileSyncLibrary.Tests;

public class DryRunTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _originPath;
    private readonly string _destinationPath;
    private readonly FileSynchronizer _synchronizer;

    public DryRunTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _originPath = Path.Combine(_testRoot, "origin");
        _destinationPath = Path.Combine(_testRoot, "destination");
        _synchronizer = new FileSynchronizer();

        Directory.CreateDirectory(_originPath);
        Directory.CreateDirectory(_destinationPath);
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_ShouldNotCreateFiles()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.Equal(0, result.FilesUpdated);
        Assert.Equal(0, result.FilesSkipped);
        Assert.True(result.IsSuccess);

        // Verify file was NOT actually created
        var destinationFile = Path.Combine(_destinationPath, "test.txt");
        Assert.False(File.Exists(destinationFile));
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_ShouldNotUpdateFiles()
    {
        // Arrange
        var originFile = Path.Combine(_originPath, "test.txt");
        var destinationFile = Path.Combine(_destinationPath, "test.txt");
        
        await File.WriteAllTextAsync(destinationFile, "Old Content");
        await Task.Delay(10); // Ensure different timestamps
        await File.WriteAllTextAsync(originFile, "New Content");

        var originalContent = await File.ReadAllTextAsync(destinationFile);

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.FilesCreated);
        Assert.Equal(1, result.FilesUpdated);
        Assert.Equal(0, result.FilesSkipped);
        Assert.True(result.IsSuccess);

        // Verify file content was NOT actually changed
        var currentContent = await File.ReadAllTextAsync(destinationFile);
        Assert.Equal(originalContent, currentContent);
        Assert.Equal("Old Content", currentContent);
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_ShouldReportSkippedFiles()
    {
        // Arrange
        var originFile = Path.Combine(_originPath, "test.txt");
        var destinationFile = Path.Combine(_destinationPath, "test.txt");
        
        await File.WriteAllTextAsync(originFile, "Same Content");
        await File.WriteAllTextAsync(destinationFile, "Same Content");
        
        // Ensure same timestamp
        var timestamp = DateTime.Now;
        File.SetLastWriteTime(originFile, timestamp);
        File.SetLastWriteTime(destinationFile, timestamp);

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.FilesCreated);
        Assert.Equal(0, result.FilesUpdated);
        Assert.Equal(1, result.FilesSkipped);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_ShouldNotCreateDirectories()
    {
        // Arrange
        var subDir = Path.Combine(_originPath, "subdir");
        Directory.CreateDirectory(subDir);
        var testFile = Path.Combine(subDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        var destinationSubDir = Path.Combine(_destinationPath, "subdir");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.True(result.IsSuccess);

        // Verify directory was NOT actually created
        Assert.False(Directory.Exists(destinationSubDir));
        Assert.False(File.Exists(Path.Combine(destinationSubDir, "test.txt")));
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_ShouldReportCorrectToString()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        var resultString = result.ToString();
        Assert.Contains("[DRY RUN]", resultString);
        Assert.Contains("1 total", resultString);
        Assert.Contains("1 created", resultString);
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_WithProgressReporting_ShouldIndicateDryRun()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        var progressReports = new List<SyncProgress>();
        var progress = new Progress<SyncProgress>(report => progressReports.Add(report));

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true, progress: progress);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.NotEmpty(progressReports);
        
        // Check that progress reports indicate dry run
        Assert.Contains(progressReports, p => p.CurrentOperation.Contains("[DRY RUN]"));
        
        // Check starting progress
        var startProgress = progressReports.First();
        Assert.Contains("[DRY RUN] Starting synchronization", startProgress.CurrentOperation);
        
        // Check completion progress
        var endProgress = progressReports.Last();
        Assert.Contains("[DRY RUN] Synchronization analysis completed", endProgress.CurrentOperation);
        
        // Check file progress
        var fileProgress = progressReports.FirstOrDefault(p => p.CurrentOperation.Contains("Would Create"));
        Assert.NotNull(fileProgress);
        Assert.Contains("[DRY RUN] Would Create:", fileProgress.CurrentOperation);
    }

    [Fact]
    public async Task SynchronizeAsync_DryRun_WithMultipleFiles_ShouldReportAllActions()
    {
        // Arrange
        // File to be created
        var newFile = Path.Combine(_originPath, "new.txt");
        await File.WriteAllTextAsync(newFile, "New File");

        // File to be updated
        var updateFile = Path.Combine(_originPath, "update.txt");
        var updateDestFile = Path.Combine(_destinationPath, "update.txt");
        await File.WriteAllTextAsync(updateDestFile, "Old Content");
        await Task.Delay(10);
        await File.WriteAllTextAsync(updateFile, "New Content");

        // File to be skipped
        var skipFile = Path.Combine(_originPath, "skip.txt");
        var skipDestFile = Path.Combine(_destinationPath, "skip.txt");
        await File.WriteAllTextAsync(skipFile, "Same Content");
        await File.WriteAllTextAsync(skipDestFile, "Same Content");
        var timestamp = DateTime.Now;
        File.SetLastWriteTime(skipFile, timestamp);
        File.SetLastWriteTime(skipDestFile, timestamp);

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: true);

        // Assert
        Assert.True(result.IsDryRun);
        Assert.Equal(3, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.Equal(1, result.FilesUpdated);
        Assert.Equal(1, result.FilesSkipped);
        Assert.True(result.IsSuccess);

        // Verify no actual file operations occurred
        Assert.False(File.Exists(Path.Combine(_destinationPath, "new.txt")));
        Assert.Equal("Old Content", await File.ReadAllTextAsync(updateDestFile));
        Assert.Equal("Same Content", await File.ReadAllTextAsync(skipDestFile));
    }

    [Fact]
    public async Task SynchronizeAsync_DryRunFalse_ShouldPerformActualOperations()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        // Act
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt", dryRun: false);

        // Assert
        Assert.False(result.IsDryRun);
        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.FilesCreated);
        Assert.True(result.IsSuccess);

        // Verify file was actually created
        var destinationFile = Path.Combine(_destinationPath, "test.txt");
        Assert.True(File.Exists(destinationFile));
        Assert.Equal("Hello World", await File.ReadAllTextAsync(destinationFile));
        
        // Verify ToString doesn't contain DRY RUN
        Assert.DoesNotContain("[DRY RUN]", result.ToString());
    }

    [Fact]
    public async Task SynchronizeAsync_DryRunDefault_ShouldBeFalse()
    {
        // Arrange
        var testFile = Path.Combine(_originPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        // Act - Don't specify dryRun parameter, should default to false
        var result = await _synchronizer.SynchronizeAsync(_originPath, _destinationPath, @".*\.txt");

        // Assert
        Assert.False(result.IsDryRun);
        Assert.True(File.Exists(Path.Combine(_destinationPath, "test.txt")));
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
