using System;
using System.ComponentModel.Design;
using System.Linq;
using CnSharp.VSIX.Git.Dialogs;
using CnSharp.VSIX.Git.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CnSharp.VSIX.Git.Commands
{
    internal sealed class DeleteBranchesCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("7b9c1b1b-5678-8765-bcde-f01234567890");

        private readonly AsyncPackage _package;

        private DeleteBranchesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var command = (OleMenuCommand)sender;
            var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var solutionFile = dte?.Solution?.FullName;
            var solutionDir = string.IsNullOrEmpty(solutionFile) ? null : System.IO.Path.GetDirectoryName(solutionFile);
            command.Enabled = GitUtils.IsInGitRepository(solutionDir);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new DeleteBranchesCommand(package, commandService!);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            if (string.IsNullOrEmpty(solutionDir)) return;

            var dialog = new DeleteBranchesWindow();
            if (dialog.ShowDialog() != true) return;

            var keyword = dialog.Keyword.Trim();
            if (string.IsNullOrEmpty(keyword)) return;

            _ = ExecuteAsync(solutionDir, keyword);
        }

        private async Task ExecuteAsync(string workingDir, string keyword)
        {
            var (branchOutput, _) = await GitUtils.RunGitAsync(workingDir, "branch");
            var currentBranch = branchOutput.Split('\n')
                .FirstOrDefault(l => l.TrimStart().StartsWith("* "))
                ?.Trim().Substring(2) ?? "";

            var allBranches = branchOutput.Split('\n')
                .Select(l => l.Trim().TrimStart('*').Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            var matching = allBranches
                .Where(b => b.Contains(keyword, StringComparison.OrdinalIgnoreCase) && b != currentBranch)
                .ToList();

            if (matching.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(_package, $"No branches found matching: {keyword}", "Delete Branches",
                    OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var skippedNote = currentBranch.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ? $"\n\n(Skipping current branch: {currentBranch})" : "";
            var preview = string.Join("\n", matching);
            var result = VsShellUtilities.ShowMessageBox(_package,
                $"The following branches will be deleted:\n\n{preview}{skippedNote}",
                "Confirm Branch Deletion",
                OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
            if (result != 1) return;

            var args = new[] { "branch", "-D" }.Concat(matching).ToArray();
            var (output, exitCode) = await GitUtils.RunGitAsync(workingDir, args);

            var icon = exitCode == 0 ? OLEMSGICON.OLEMSGICON_INFO : OLEMSGICON.OLEMSGICON_CRITICAL;
            var title = exitCode == 0 ? "Delete Branches" : "Delete Branches - Error";
            VsShellUtilities.ShowMessageBox(_package, string.IsNullOrEmpty(output) ? "Done." : output,
                title, icon, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
