using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FlashFiles.Models
{
    public class SyncSettings
    {
        public string SourceDirectory { get; set; } = string.Empty;
        public string DestinationDirectory { get; set; } = string.Empty;
        public string IncludePatterns { get; set; } = ".*";
        public string ExcludePatterns { get; set; } = string.Empty;
        public int MaxConcurrency { get; set; } = 5;
        public bool DryRun { get; set; } = false;
        public bool AutoScroll { get; set; } = true;
        
        // Recently used directories for quick access
        public List<string> RecentSourceDirectories { get; set; } = new();
        public List<string> RecentDestinationDirectories { get; set; } = new();
        
        // Window state
        public double WindowWidth { get; set; } = 1000;
        public double WindowHeight { get; set; } = 700;
        public bool WindowMaximized { get; set; } = false;
        
        public SyncSettings Clone()
        {
            var json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<SyncSettings>(json) ?? new SyncSettings();
        }
        
        public void AddRecentSourceDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return;
            
            RecentSourceDirectories.Remove(directory);
            RecentSourceDirectories.Insert(0, directory);
            
            // Keep only the last 10 entries
            if (RecentSourceDirectories.Count > 10)
                RecentSourceDirectories.RemoveRange(10, RecentSourceDirectories.Count - 10);
        }
        
        public void AddRecentDestinationDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return;
            
            RecentDestinationDirectories.Remove(directory);
            RecentDestinationDirectories.Insert(0, directory);
            
            // Keep only the last 10 entries
            if (RecentDestinationDirectories.Count > 10)
                RecentDestinationDirectories.RemoveRange(10, RecentDestinationDirectories.Count - 10);
        }
    }
}
