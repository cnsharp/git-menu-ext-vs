using System;
using System.ComponentModel.Design;
using System.Windows;
using CnSharp.VSIX.Git.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CnSharp.VSIX.Git.Commands
{
    internal sealed class CopyCommitIdCommand
    {
        public const int CommandId = 0x0500;
        public static readonly Guid CommandSet = new Guid("7b9c1b1b-5678-8765-bcde-f01234567890");

        private readonly AsyncPackage _package;

        private CopyCommitIdCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            new CopyCommitIdCommand(package, commandService!);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var sha = GitHistorySelection.GetSelectedCommitSha();
            if (string.IsNullOrEmpty(sha))
            {
                SetStatus("Copy Commit ID: No selected commit found.");
                return;
            }

            try { Clipboard.SetDataObject(sha, true); }
            catch { /* Clipboard temporarily busy, ignore */ }

            SetStatus($"Commit ID copied: {sha}");
        }

        private void SetStatus(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Package.GetGlobalService(typeof(SVsStatusbar)) is IVsStatusbar bar)
            {
                bar.FreezeOutput(0);
                bar.SetText(text);
            }
        }
    }
}
