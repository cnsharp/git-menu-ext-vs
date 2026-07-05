using System;
using System.Runtime.InteropServices;
using System.Threading;
using CnSharp.VSIX.Git.Commands;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CnSharp.VSIX.Git
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("6f8a0a0a-1234-4321-abcd-ef0123456789")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class GitMenuExtPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await DeleteBranchesCommand.InitializeAsync(this);
            await DeleteOutdatedBranchesCommand.InitializeAsync(this);
            await CopyCommitIdCommand.InitializeAsync(this);
            await ExportChangedFilesCommand.InitializeAsync(this);
        }
    }
}
