using System.Configuration;

namespace CnSharp.VSIX.Git.Dialogs
{
    [SettingsGroupName("CnSharp.VSIX.Git.Dialogs.Settings")]
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static readonly Settings _default = (Settings)Synchronized(new Settings());
        public static Settings Default => _default;

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string DeleteBranchesKeyword
        {
            get => (string)this[nameof(DeleteBranchesKeyword)];
            set => this[nameof(DeleteBranchesKeyword)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string ExportExtensions
        {
            get => (string)this[nameof(ExportExtensions)];
            set => this[nameof(ExportExtensions)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("")]
        public string ExportOutputDir
        {
            get => (string)this[nameof(ExportOutputDir)];
            set => this[nameof(ExportOutputDir)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool ExportAsZip
        {
            get => (bool)this[nameof(ExportAsZip)];
            set => this[nameof(ExportAsZip)] = value;
        }
    }
}
