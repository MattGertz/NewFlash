using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FileSyncLibrary;
using FlashFiles.Models;
using FlashFiles.Services;

namespace FlashFiles.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private SyncSettings _settings;
        private FileSynchronizer _synchronizer;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isSyncing;
        private int _currentProgress;
        private string _currentStatus = "Ready";
        private string _currentFile = string.Empty;

        public MainWindowViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = new SyncSettings();
            _synchronizer = new FileSynchronizer();
            
            // Commands
            BrowseSourceCommand = new RelayCommand(BrowseSource);
            BrowseDestinationCommand = new RelayCommand(BrowseDestination);
            StartSyncCommand = new RelayCommand(async () => await StartSyncAsync(), () => CanStartSync());
            StopSyncCommand = new RelayCommand(StopSync, () => IsSyncing);
            ClearLogCommand = new RelayCommand(ClearLog);
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
            
            // Log collection
            LogEntries = new ObservableCollection<string>();
            
            // Load settings
            _ = LoadSettingsAsync();
        }

        #region Properties

        public string SourceDirectory
        {
            get => _settings.SourceDirectory;
            set
            {
                if (_settings.SourceDirectory != value)
                {
                    _settings.SourceDirectory = value;
                    OnPropertyChanged();
                    ((RelayCommand)StartSyncCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string DestinationDirectory
        {
            get => _settings.DestinationDirectory;
            set
            {
                if (_settings.DestinationDirectory != value)
                {
                    _settings.DestinationDirectory = value;
                    OnPropertyChanged();
                    ((RelayCommand)StartSyncCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string IncludePatterns
        {
            get => _settings.IncludePatterns;
            set
            {
                if (_settings.IncludePatterns != value)
                {
                    _settings.IncludePatterns = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExcludePatterns
        {
            get => _settings.ExcludePatterns;
            set
            {
                if (_settings.ExcludePatterns != value)
                {
                    _settings.ExcludePatterns = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxConcurrency
        {
            get => _settings.MaxConcurrency;
            set
            {
                if (_settings.MaxConcurrency != value)
                {
                    _settings.MaxConcurrency = Math.Max(1, Math.Min(20, value));
                    OnPropertyChanged();
                }
            }
        }

        public bool DryRun
        {
            get => _settings.DryRun;
            set
            {
                if (_settings.DryRun != value)
                {
                    _settings.DryRun = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoScroll
        {
            get => _settings.AutoScroll;
            set
            {
                if (_settings.AutoScroll != value)
                {
                    _settings.AutoScroll = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                if (_isSyncing != value)
                {
                    _isSyncing = value;
                    OnPropertyChanged();
                    ((RelayCommand)StartSyncCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StopSyncCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public int CurrentProgress
        {
            get => _currentProgress;
            set
            {
                if (_currentProgress != value)
                {
                    _currentProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (_currentStatus != value)
                {
                    _currentStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    _currentFile = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> LogEntries { get; }

        #endregion

        #region Commands

        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseDestinationCommand { get; }
        public ICommand StartSyncCommand { get; }
        public ICommand StopSyncCommand { get; }
        public ICommand ClearLogCommand { get; }        public ICommand SaveSettingsCommand { get; }

        #endregion

        #region Command Implementations

        private void BrowseSource()
        {
            var selectedPath = SelectFolder("Select Source Directory", SourceDirectory);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                SourceDirectory = selectedPath;
                _settings.AddRecentSourceDirectory(SourceDirectory);
            }
        }

        private void BrowseDestination()
        {
            var selectedPath = SelectFolder("Select Destination Directory", DestinationDirectory);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                DestinationDirectory = selectedPath;
                _settings.AddRecentDestinationDirectory(DestinationDirectory);
            }
        }

        private bool CanStartSync()
        {
            return !IsSyncing && 
                   !string.IsNullOrWhiteSpace(SourceDirectory) && 
                   !string.IsNullOrWhiteSpace(DestinationDirectory) &&
                   Directory.Exists(SourceDirectory);
        }

        private async Task StartSyncAsync()
        {
            if (IsSyncing) return;

            try
            {
                IsSyncing = true;
                CurrentProgress = 0;
                CurrentStatus = DryRun ? "Starting dry run..." : "Starting synchronization...";
                CurrentFile = string.Empty;

                _cancellationTokenSource = new CancellationTokenSource();

                // Configure progress reporting
                var progress = new Progress<SyncProgress>(OnProgressUpdate);

                // Add start message to log
                var modeText = DryRun ? "DRY RUN" : "SYNC";
                AddLogEntry($"=== {modeText} STARTED ===");
                AddLogEntry($"Source: {SourceDirectory}");
                AddLogEntry($"Destination: {DestinationDirectory}");
                AddLogEntry($"Include Patterns: {IncludePatterns}");
                if (!string.IsNullOrWhiteSpace(ExcludePatterns))
                    AddLogEntry($"Exclude Patterns: {ExcludePatterns}");
                AddLogEntry($"Max Concurrency: {MaxConcurrency}");
                AddLogEntry("");

                // Start synchronization
                var result = await _synchronizer.SynchronizeAsync(
                    SourceDirectory,
                    DestinationDirectory,
                    IncludePatterns,
                    0, // maxRetries
                    DryRun,
                    progress,
                    _cancellationTokenSource.Token);                // Show results
                AddLogEntry("");
                AddLogEntry("=== RESULTS ===");
                AddLogEntry($"Files processed: {result.TotalFiles}");
                AddLogEntry($"Files created: {result.FilesCreated}");
                AddLogEntry($"Files updated: {result.FilesUpdated}");
                AddLogEntry($"Files skipped: {result.FilesSkipped}");
                AddLogEntry($"Files failed: {result.FilesFailed}");
                AddLogEntry($"Errors: {result.Errors.Count}");
                if (result.TotalRetryAttempts > 0)
                    AddLogEntry($"Retry attempts: {result.TotalRetryAttempts}");

                if (result.Errors.Count > 0)
                {
                    AddLogEntry("");
                    AddLogEntry("=== ERRORS ===");
                    foreach (var error in result.Errors)
                    {
                        AddLogEntry($"ERROR: {error}");
                    }
                }

                CurrentStatus = result.Errors.Count > 0 ? "Completed with errors" : "Completed successfully";
                CurrentProgress = 100;
                CurrentFile = string.Empty;

                // Save settings after successful operation
                await SaveSettingsAsync();
            }
            catch (OperationCanceledException)
            {
                AddLogEntry("");
                AddLogEntry("=== OPERATION CANCELLED ===");
                CurrentStatus = "Cancelled";
                CurrentFile = string.Empty;
            }
            catch (Exception ex)
            {
                AddLogEntry("");
                AddLogEntry($"=== FATAL ERROR ===");
                AddLogEntry($"ERROR: {ex.Message}");
                CurrentStatus = "Error occurred";
                CurrentFile = string.Empty;
            }
            finally
            {
                IsSyncing = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void StopSync()
        {
            _cancellationTokenSource?.Cancel();
            CurrentStatus = "Cancelling...";
        }

        private void ClearLog()
        {
            LogEntries.Clear();
            CurrentProgress = 0;
            CurrentStatus = "Ready";            CurrentFile = string.Empty;
        }

        private async Task SaveSettingsAsync()
        {
            await _settingsService.SaveSettingsAsync(_settings);
        }

        #endregion

        #region Helper Methods

        private string SelectFolder(string title, string initialPath)
        {
            // Simple folder selection using WPF MessageBox for now
            // In a production app, you might want to use a more sophisticated dialog
            var result = MessageBox.Show(
                $"{title}\n\nCurrent path: {initialPath}\n\nPress OK to browse for a folder or Cancel to use current path.",
                "Select Folder",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                // For now, we'll return the initial path or prompt for manual entry
                // This is a temporary solution until we implement a proper folder browser
                return initialPath ?? string.Empty;
            }

            return string.Empty;
        }

        private void OnProgressUpdate(SyncProgress progress)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentProgress = (int)progress.PercentComplete;
                CurrentStatus = progress.CurrentOperation;
                CurrentFile = $"Processing file {progress.ProcessedFiles}/{progress.TotalFiles}";

                // Add progress message to log
                AddLogEntry($"{progress.ProcessedFiles}/{progress.TotalFiles} - {progress.CurrentOperation}");
            });
        }

        private void AddLogEntry(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            LogEntries.Add($"[{timestamp}] {message}");

            if (AutoScroll)
            {
                // Scroll to the last item (handled in the view)
                OnPropertyChanged(nameof(LogEntries));
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        private async Task LoadSettingsAsync()
        {
            _settings = await _settingsService.LoadSettingsAsync();
            
            // Notify all properties changed
            OnPropertyChanged(nameof(SourceDirectory));
            OnPropertyChanged(nameof(DestinationDirectory));
            OnPropertyChanged(nameof(IncludePatterns));
            OnPropertyChanged(nameof(ExcludePatterns));
            OnPropertyChanged(nameof(MaxConcurrency));
            OnPropertyChanged(nameof(DryRun));
            OnPropertyChanged(nameof(AutoScroll));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
