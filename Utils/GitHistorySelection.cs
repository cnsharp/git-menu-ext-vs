using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CnSharp.VSIX.Git.Utils
{
    public sealed class HistoryCommit
    {
        public string Sha { get; set; }
        public DateTimeOffset Time { get; set; }
    }

    /// <summary>
    /// Reads the currently selected commit from the Git history tool window (pure WPF).
    /// Walks the visual tree to find a Selector (ListView/DataGrid) whose SelectedItem
    /// is a GitHistoryCommitItem, then reads Id (IGitId).ToString() = full SHA and
    /// CommitTime = commit time.
    /// ⚠ Relies on VS internal types; may break after VS upgrades.
    /// </summary>
    public static class GitHistorySelection
    {
        private static readonly Regex ShaLike = new Regex("^[0-9a-fA-F]{7,40}$", RegexOptions.Compiled);

        /// <summary>The single commit under right-click/focus (SelectedItem).</summary>
        public static string GetSelectedCommitSha()
        {
            var sel = FindCommitSelector();
            return sel == null ? null : ShaOf(sel.SelectedItem);
        }

        /// <summary>All selected commits (SelectedItems), returned in descending order by commit time (newest first, consistent with history view).</summary>
        public static IReadOnlyList<HistoryCommit> GetSelectedCommits()
        {
            var result = new List<HistoryCommit>();
            var sel = FindCommitSelector();
            if (sel == null) return result;

            IEnumerable<object> items;
            var selectedItems = sel.GetType().GetProperty("SelectedItems")?.GetValue(sel) as System.Collections.IList;
            if (selectedItems != null && selectedItems.Count > 0)
                items = selectedItems.Cast<object>();
            else
                items = new[] { sel.SelectedItem };

            foreach (var it in items)
            {
                var sha = ShaOf(it);
                if (sha == null) continue;
                result.Add(new HistoryCommit { Sha = sha, Time = TimeOf(it) });
            }
            return result.OrderByDescending(c => c.Time).ToList();
        }

        private static string ShaOf(object item)
        {
            if (item == null) return null;
            var sha = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
            return (!string.IsNullOrEmpty(sha) && ShaLike.IsMatch(sha)) ? sha : null;
        }

        private static DateTimeOffset TimeOf(object item)
        {
            var val = item.GetType().GetProperty("CommitTime")?.GetValue(item);
            return val is DateTimeOffset dto ? dto : DateTimeOffset.MinValue;
        }

        private static Selector FindCommitSelector()
        {
            if (Application.Current == null) return null;
            foreach (Window w in Application.Current.Windows)
            {
                var s = Walk(w);
                if (s != null) return s;
            }
            return null;
        }

        private static Selector Walk(DependencyObject obj)
        {
            if (obj == null) return null;
            if (obj is Selector sel && sel.SelectedItem != null && IsCommit(sel.SelectedItem))
                return sel;

            if (obj is Visual || obj is System.Windows.Media.Media3D.Visual3D)
            {
                int n = VisualTreeHelper.GetChildrenCount(obj);
                for (int i = 0; i < n; i++)
                {
                    var r = Walk(VisualTreeHelper.GetChild(obj, i));
                    if (r != null) return r;
                }
            }
            return null;
        }

        private static bool IsCommit(object item)
        {
            var tn = item?.GetType().FullName ?? "";
            return tn.IndexOf("Commit", StringComparison.OrdinalIgnoreCase) >= 0
                   && item.GetType().GetProperty("Id") != null;
        }
    }
}
