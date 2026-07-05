# Git Menu Ext

A Visual Studio extension (VSIX) that adds a few handy Git actions to the built-in **Git** menu and the **commit history context menu**.

## Features

### Top-level **Git** menu
> Both commands are automatically greyed out when the current solution is not under Git control.

| Command | Description |
|---------|-------------|
| **Delete Branches…** | Bulk-delete local branches matching a keyword (lists matches with a confirmation step; the current branch is skipped). |
| **Delete Outdated Branches…** | List and delete local branches whose upstream/remote branch has been deleted (`gone`). Local branches that never tracked a remote are left untouched. Pick which ones to delete. |

### Commit history context menu
> Location: **Git Repository window → commit history**, right-click one or more commits.

| Command | Description |
|---------|-------------|
| **Copy Commit ID** | Copies the selected commit's full 40-char SHA to the clipboard (with a status-bar confirmation). |
| **Export Changed Files…** | Supports **Ctrl multi-select**. Shows a dialog listing the selected commits (newest → oldest, with date/time), then exports the **union of files changed by each selected commit**. Optional extension filter and zip packaging. |

## Requirements

- Visual Studio 2022 (`[17.0, 19.0)`) — Community / Professional / Enterprise
- Windows, `amd64`
- .NET Framework 4.7.2
- `git` available on `PATH`

## Build / Debug

```powershell
# Build with MSBuild
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' GitMenuExt.csproj /t:Rebuild /p:Configuration=Debug
```

Or open the solution in VS and press **F5** to launch the experimental instance (`/rootsuffix Exp`).

> ⚠️ **After changing the menu (`.vsct`)**, VS caches the merged menu — reset the experimental instance to see the changes:
> ```
> "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CreateExpInstance.exe" /Reset /VSInstance=18.0 /RootSuffix=Exp
> ```
> Not needed for C#/XAML-only changes.

## Implementation notes

- **Menu placement**: commands are parented under Visual Studio's built-in Git command set `{57735D06-C920-4415-A2E0-7D6E6FBDFA99}` — the top-level Git root menu `0xF000` and the commit history context menu `0xF040`. These ids are not publicly documented; they were confirmed via VSIP Logging.
- **Reading the selected commit(s)**: the history window is pure WPF and does not expose its selection through command arguments or the classic selection service (`ISelectionContainer`). The extension walks the WPF visual tree to find the `Selector` whose `SelectedItem` is a `Microsoft.TeamFoundation.Git.Controls.History.GitHistoryCommitItem`, and reads its `Id` (`IGitId`) to get the SHA.
  > ⚠️ This is an **unsupported** approach that relies on VS internal types; it may break on a major VS upgrade, in which case the type/property names need to be re-confirmed.

## Project layout

```
Commands/         Commands (DeleteBranches / DeleteOutdatedBranches / CopyCommitId / ExportSelectedCommits)
Dialogs/          WPF dialogs (delete branches, export selected commits, …)
Utils/            GitUtils (git invocation), GitHistorySelection (read selected commits), ExportChangedFilesCore (export core)
GitMenuExtPackage.vsct   Menu / command-table definition
GitMenuExtPackage.cs     AsyncPackage entry point
```

## License

Released under the [MIT License](LICENSE).
