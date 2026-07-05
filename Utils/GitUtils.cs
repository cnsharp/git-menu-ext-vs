using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CnSharp.VSIX.Git.Utils
{
    public static class GitUtils
    {
        /// <summary>
        /// Returns true if <paramref name="path"/> is inside a git working tree,
        /// i.e. a ".git" folder (or file, for worktrees/submodules) exists at it or any ancestor.
        /// Pure filesystem check — safe to call from BeforeQueryStatus on the UI thread.
        /// </summary>
        public static bool IsInGitRepository(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                for (var dir = new DirectoryInfo(path); dir != null; dir = dir.Parent)
                {
                    var gitPath = Path.Combine(dir.FullName, ".git");
                    if (Directory.Exists(gitPath) || File.Exists(gitPath)) return true;
                }
            }
            catch { /* ignore path/permission errors -> treat as not a repo */ }
            return false;
        }

        public static async Task<(string Output, int ExitCode)> RunGitAsync(string workingDir, params string[] args)
        {
            var psi = new ProcessStartInfo("git")
            {
                WorkingDirectory = workingDir,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var proc = new Process { StartInfo = psi };
            var sb = new StringBuilder();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await Task.Run(() => proc.WaitForExit());

            return (sb.ToString().Trim(), proc.ExitCode);
        }
    }
}
