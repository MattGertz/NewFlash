using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FlashFiles.Models;

namespace FlashFiles.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private SyncSettings _currentSettings;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "FlashFiles");
            Directory.CreateDirectory(appFolder);
            
            _settingsPath = Path.Combine(appFolder, "settings.json");
            _currentSettings = new SyncSettings();
        }

        public async Task<SyncSettings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    var settings = JsonSerializer.Deserialize<SyncSettings>(json);
                    _currentSettings = settings ?? new SyncSettings();
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with default settings
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                _currentSettings = new SyncSettings();
            }

            return _currentSettings.Clone();
        }

        public async Task SaveSettingsAsync(SyncSettings settings)
        {
            try
            {
                _currentSettings = settings.Clone();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(_currentSettings, options);
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - settings persistence should not break the app
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public SyncSettings GetCurrentSettings()
        {
            return _currentSettings.Clone();
        }
    }
}
