using System.Windows;

namespace CnSharp.VSIX.Git.Dialogs
{
    public partial class DeleteBranchesWindow : Window
    {
        public string Keyword { get; private set; } = string.Empty;

        public DeleteBranchesWindow()
        {
            InitializeComponent();
            KeywordBox.Text = Settings.Default.DeleteBranchesKeyword;
            KeywordBox.Focus();
            KeywordBox.SelectAll();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Keyword = KeywordBox.Text;
            Settings.Default.DeleteBranchesKeyword = Keyword;
            Settings.Default.Save();
            DialogResult = true;
        }
    }
}
