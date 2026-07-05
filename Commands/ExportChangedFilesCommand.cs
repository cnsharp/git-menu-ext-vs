using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using CnSharp.VSIX.Git.Dialogs;
using CnSharp.VSIX.Git.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CnSharp.VSIX.Git.Commands
{
    internal sealed class ExportChangedFilesCommand
    {
        public const int CommandId = 0x0600;
        public static readonly Guid CommandSet = new Guid("7b9c1b1b-5678-8765-bcde-f01234567890");

        private readonly AsyncPackage _package;

        private ExportChangedFilesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new ExportChangedFilesCommand(package, commandService!);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Read multi-selected commits from the WPF history list (UI thread)
            var commits = GitHistorySelection.GetSelectedCommits();
            if (commits.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(_package, "Please select one or more commits in the history first.",
                    "Export Changed Files", OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var solutionFile = dte?.Solution?.FullName;
            var solutionDir = string.IsNullOrEmpty(solutionFile) ? null : Path.GetDirectoryName(solutionFile);
            if (string.IsNullOrEmpty(solutionDir))
            {
                VsShellUtilities.ShowMessageBox(_package, "No open solution found.",
                    "Export Changed Files", OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var projectName = Path.GetFileNameWithoutExtension(solutionFile);
            _ = RunFlowAsync(solutionDir, projectName, commits.ToList());
        }

        private async Task RunFlowAsync(string workingDir, string projectName, List<HistoryCommit> commits)
        {
            // Get each commit's subject for display (commits are already in ascending order by time).
            // Use tab as delimiter (no spaces in tokens to avoid being split by RunGitAsync's space
            // concatenation); git outputs %x09 as a real tab.
            var args = new List<string> { "show", "-s", "--format=%H%x09%s" };
            args.AddRange(commits.Select(c => c.Sha));
            var (output, _) = await GitUtils.RunGitAsync(workingDir, args.ToArray());

            var subjects = new Dictionary<string, string>();
            foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;
                subjects[parts[0].Trim()] = parts[1].Trim();
            }

            var lines = commits.Select(c =>
            {
                var shortSha = c.Sha.Length >= 7 ? c.Sha.Substring(0, 7) : c.Sha;
                var subject = subjects.TryGetValue(c.Sha, out var s) ? s : "";
                return $"{shortSha}  {c.Time.LocalDateTime:yyyy-MM-dd HH:mm}  {subject}";
            }).ToList();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialog = new ExportSelectedCommitsWindow(lines);
            if (dialog.ShowDialog() != true) return;

            var extensions = dialog.Extensions.Trim();
            var outputDir = dialog.OutputDir.Trim();
            if (string.IsNullOrEmpty(outputDir)) return;

            // Union of changed files from each selected commit
            var shas = commits.Select(c => c.Sha).ToList();
            var outputPath = await ExportChangedFilesCore.RunForCommitsAsync(_package, workingDir, projectName,
                shas, extensions, outputDir, dialog.AsZip);

            if (outputPath == null) return;

            // Open in Explorer: folder directly, zip file with /select to highlight it
            if (dialog.AsZip)
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
            else
                System.Diagnostics.Process.Start("explorer.exe", $"\"{outputPath}\"");
        }
    }
}
