using System.Windows;
using FlashFiles.ViewModels;
using FlashFiles.Services;

namespace FlashFiles
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services
            var settingsService = new SettingsService();
            
            // Create and show main window
            var mainWindow = new MainWindow();
            var viewModel = new MainWindowViewModel(settingsService);
            mainWindow.DataContext = viewModel;
            
            mainWindow.Show();
        }
    }
}
