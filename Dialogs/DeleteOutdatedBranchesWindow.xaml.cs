using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CnSharp.VSIX.Git.Commands;

namespace CnSharp.VSIX.Git.Dialogs
{
    public partial class DeleteOutdatedBranchesWindow : Window
    {
        public List<OutdatedBranch> SelectedBranches { get; private set; } = new();

        private readonly List<BranchItem> _items;

        public DeleteOutdatedBranchesWindow(List<OutdatedBranch> branches)
        {
            InitializeComponent();
            _items = branches.Select(b => new BranchItem(b)).ToList();
            BranchList.ItemsSource = _items;
            // select all by default
            foreach (var item in BranchList.Items)
                BranchList.SelectedItems.Add(item);
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            SelectedBranches = BranchList.SelectedItems.Cast<BranchItem>().Select(i => i.Branch).ToList();
            DialogResult = true;
        }
    }

    public class BranchItem
    {
        public OutdatedBranch Branch { get; }
        public string DisplayName => Branch.IsMerged ? Branch.Name : $"{Branch.Name}  ⚠ not fully merged";

        public BranchItem(OutdatedBranch branch) => Branch = branch;
    }
}
