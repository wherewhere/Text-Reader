using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace TextReader.Helpers
{
    internal static partial class SettingsHelper
    {
        public const string SelectedAppTheme = "SelectedAppTheme";

        public static Type Get<Type>(string key) => (Type)LocalSettings.Values[key];

        public static void Set(string key, object value) => LocalSettings.Values[key] = value;

        public static void SetDefaultSettings()
        {
            if (!LocalSettings.Values.ContainsKey(SelectedAppTheme))
            {
                LocalSettings.Values.Add(SelectedAppTheme, (int)ElementTheme.Default);
            }
        }
    }

    internal static partial class SettingsHelper
    {
        public static ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;
        public static double WindowsVersion = double.Parse($"{(ushort)((version & 0x00000000FFFF0000L) >> 16)}.{(ushort)(SettingsHelper.version & 0x000000000000FFFFL)}");

        static SettingsHelper()
        {
            SetDefaultSettings();
        }
    }
}
