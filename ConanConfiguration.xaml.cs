using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace conan_vs_extension
{
    public partial class ConanConfiguration : UserControl
    {
        public string ConanExecutablePath { get; private set; }
        public bool UseSystemConan { get; private set; }

        public ConanConfiguration()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PathTextBox.Text = openFileDialog.FileName;
                UseSystemConanCheckBox.IsChecked = false;
            }
        }

        private void UseSystemConanCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PathTextBox.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            PathTextBox.Clear();

        }

        private void UseSystemConanCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PathTextBox.IsEnabled = true;
            BrowseButton.IsEnabled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ConanExecutablePath = PathTextBox.Text;
            UseSystemConan = UseSystemConanCheckBox.IsChecked ?? false;

            Window parentWindow = Window.GetWindow(this);
            parentWindow.DialogResult = true;
            parentWindow.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.DialogResult = false;
            parentWindow.Close();
        }
    }
}
