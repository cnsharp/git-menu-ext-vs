using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using CnSharp.VSIX.Git.Dialogs;
using CnSharp.VSIX.Git.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CnSharp.VSIX.Git.Commands
{
    internal sealed class DeleteOutdatedBranchesCommand
    {
        public const int CommandId = 0x0200;
        public static readonly Guid CommandSet = new Guid("7b9c1b1b-5678-8765-bcde-f01234567890");

        private readonly AsyncPackage _package;

        private DeleteOutdatedBranchesCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            new DeleteOutdatedBranchesCommand(package, commandService!);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            if (string.IsNullOrEmpty(solutionDir)) return;

            _ = ExecuteAsync(solutionDir);
        }

        private async Task ExecuteAsync(string workingDir)
        {
            var (vvOutput, _) = await GitUtils.RunGitAsync(workingDir, "branch", "-vv");
            var (mergedOutput, _) = await GitUtils.RunGitAsync(workingDir, "branch", "--merged");
            var (headOutput, _) = await GitUtils.RunGitAsync(workingDir, "rev-parse", "--abbrev-ref", "HEAD");

            var currentBranch = headOutput.Trim();
            var mergedBranches = mergedOutput.Split('\n')
                .Select(l => l.Trim().TrimStart('*').Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var outdated = new List<OutdatedBranch>();
            foreach (var line in vvOutput.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmed = line.Trim();
                var name = trimmed.TrimStart('*').Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (string.IsNullOrEmpty(name) || name == currentBranch) continue;

                // Only keep branches that have an upstream but whose remote has been deleted (gone);
                // Branches that never set tracking (no [..]) are NOT considered outdated and won't be deleted.
                var isGone = System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\[[^\]]*:\s*gone\]");

                if (isGone)
                    outdated.Add(new OutdatedBranch(name, mergedBranches.Contains(name)));
            }

            if (outdated.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(_package, "No outdated branches found.", "Delete Outdated Branches",
                    OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            List<OutdatedBranch> selected = null!;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dialog = new DeleteOutdatedBranchesWindow(outdated);
            if (dialog.ShowDialog() != true) return;
            selected = dialog.SelectedBranches;

            if (selected.Count == 0) return;

            var results = new System.Text.StringBuilder();
            var hasError = false;
            foreach (var branch in selected)
            {
                var flag = branch.IsMerged ? "-d" : "-D";
                var (output, exitCode) = await GitUtils.RunGitAsync(workingDir, "branch", flag, branch.Name);
                if (exitCode == 0) results.AppendLine(output);
                else { results.AppendLine($"Failed to delete {branch.Name}: {output}"); hasError = true; }
            }

            var msg = results.ToString().Trim();
            var icon2 = hasError ? OLEMSGICON.OLEMSGICON_CRITICAL : OLEMSGICON.OLEMSGICON_INFO;
            VsShellUtilities.ShowMessageBox(_package, string.IsNullOrEmpty(msg) ? "Done." : msg,
                "Delete Outdated Branches", icon2, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    public class OutdatedBranch
    {
        public OutdatedBranch(string name, bool isMerged)
        {
            Name = name;
            IsMerged = isMerged;
        }

        public string Name { get; set; }
        public bool IsMerged { get; set; }
    }
}
