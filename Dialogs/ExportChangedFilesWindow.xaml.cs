using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;

namespace CnSharp.VSIX.Git.Dialogs
{
    public partial class ExportSelectedCommitsWindow : Window
    {
        public string Extensions { get; private set; } = string.Empty;
        public string OutputDir { get; private set; } = string.Empty;
        public bool AsZip { get; private set; }

        public ExportSelectedCommitsWindow(IEnumerable<string> commitLines)
        {
            InitializeComponent();
            CommitsList.ItemsSource = commitLines;
            ExtensionsBox.Text = Settings.Default.ExportExtensions;
            OutputDirBox.Text = Settings.Default.ExportOutputDir;
            ZipCheckBox.IsChecked = Settings.Default.ExportAsZip;
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = OutputDirBox.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                OutputDirBox.Text = dialog.SelectedPath;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Extensions = ExtensionsBox.Text;
            OutputDir = OutputDirBox.Text;
            AsZip = ZipCheckBox.IsChecked == true;

            Settings.Default.ExportExtensions = Extensions;
            Settings.Default.ExportOutputDir = OutputDir;
            Settings.Default.ExportAsZip = AsZip;
            Settings.Default.Save();

            DialogResult = true;
        }
    }
}
