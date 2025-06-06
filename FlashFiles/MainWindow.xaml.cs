using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using FlashFiles.ViewModels;
using FlashFiles.Services;

namespace FlashFiles
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel(new SettingsService());
            DataContext = _viewModel;
              // Set the window icon programmatically
            try
            {
                var iconUri = new Uri("pack://application:,,,/FlashFiles.ico");
                Icon = new BitmapImage(iconUri);
            }
            catch (Exception ex)
            {
                // Log the error but continue without icon
                System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
            }
            
            // Handle window events to save state
            SizeChanged += MainWindow_SizeChanged;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_viewModel != null && WindowState == WindowState.Normal)
            {
                _viewModel.WindowWidth = ActualWidth;
                _viewModel.WindowHeight = ActualHeight;
                // Auto-save settings when window size changes
                await _viewModel.SaveSettingsAsync();
            }
        }

        private async void MainWindow_StateChanged(object sender, System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.WindowState = WindowState;
                // Auto-save settings when window state changes
                await _viewModel.SaveSettingsAsync();
            }
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel != null)
            {
                // Save settings when the application is closing
                await _viewModel.SaveSettingsAsync();
            }
        }

        private void LogListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Auto-scroll to the last item when new entries are added
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox?.Items.Count > 0)
            {
                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
            }
        }
    }
}
