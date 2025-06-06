using System;
using System.IO;
using System.Windows;

namespace FlashFiles
{
    public partial class App : Application
    {
        private readonly string _logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "FlashFiles", 
            "startup.log"
        );

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Ensure log directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
                
                LogMessage("FlashFiles starting up...");
                base.OnStartup(e);

                // Create and show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                LogMessage("FlashFiles startup completed successfully.");
            }
            catch (Exception ex)
            {
                LogMessage($"Startup error: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");
                LogMessage($"Inner exception: {ex.InnerException?.Message}");
                
                MessageBox.Show($"Startup error: {ex.Message}\n\nLog file: {_logFilePath}\n\nStack trace:\n{ex.StackTrace}", 
                    "FlashFiles Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogMessage($"Unhandled exception: {e.Exception.Message}");
            LogMessage($"Stack trace: {e.Exception.StackTrace}");
            LogMessage($"Inner exception: {e.Exception.InnerException?.Message}");
            
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nLog file: {_logFilePath}\n\nStack trace:\n{e.Exception.StackTrace}", 
                "FlashFiles Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown(1);
        }

        private void LogMessage(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // If logging fails, we can't do much about it
            }
        }
    }
}
