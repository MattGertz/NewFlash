using System.Windows;
using FlashFiles.ViewModels;
using FlashFiles.Services;

namespace FlashFiles
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(new SettingsService());
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
