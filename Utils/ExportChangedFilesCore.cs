using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CnSharp.VSIX.Git.Utils
{
    /// <summary>
    /// Shared logic for exporting changed files: RunForCommitsAsync takes the union
    /// of changed files from each selected commit.
    /// </summary>
    internal static class ExportChangedFilesCore
    {
        /// <summary>
        /// Exports changed files for the given commits. Returns the output path (folder or zip), or null if nothing was exported.
        /// </summary>
        public static async Task<string?> RunForCommitsAsync(AsyncPackage package, string workingDir, string projectName,
            IReadOnlyList<string> shas, string extensions, string outputDir, bool asZip)
        {
            var files = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sha in shas)
            {
                // Files changed in this commit relative to its parent (--root allows listing for the initial commit)
                var (o, code) = await GitUtils.RunGitAsync(workingDir,
                    "diff-tree", "--no-commit-id", "--name-only", "-r", "--root", "--diff-filter=ACMR", sha);
                if (code != 0) continue;
                foreach (var f in o.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
                    if (seen.Add(f)) files.Add(f);
            }

            var baseName = shas.Count == 1
                ? $"{projectName}-{Short(shas[0])}"
                : $"{projectName}-{Short(shas[0])}-{Short(shas[shas.Count - 1])}";
            return ExportFiles(package, workingDir, baseName, files, extensions, outputDir, asZip);
        }

        private static string Short(string sha) => sha != null && sha.Length >= 7 ? sha.Substring(0, 7) : sha;

        private static string? ExportFiles(AsyncPackage package, string workingDir, string baseName,
            List<string> files, string extensions, string outputDir, bool asZip)
        {
            if (files.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(package, "No changed files found in the selected commit(s).",
                    "Export Changed Files", OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return null;
            }

            var extList = extensions.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().TrimStart('.'))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var filteredFiles = extList.Count == 0
                ? files
                : files.Where(f => extList.Any(ext => f.EndsWith($".{ext}", StringComparison.OrdinalIgnoreCase))).ToList();

            if (filteredFiles.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(package, "No files matched the specified extensions.",
                    "Export Changed Files", OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return null;
            }

            if (asZip)
            {
                var zipPath = Path.Combine(outputDir, $"{baseName}.zip");
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                foreach (var rel in filteredFiles)
                {
                    var full = Path.Combine(workingDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full)) continue;
                    zip.CreateEntryFromFile(full, rel);
                }
                return zipPath;
            }
            else
            {
                var destRoot = Path.Combine(outputDir, baseName);
                foreach (var rel in filteredFiles)
                {
                    var src = Path.Combine(workingDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(src)) continue;
                    var dest = Path.Combine(destRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(src, dest, overwrite: true);
                }
                return destRoot;
            }
        }
    }
}
