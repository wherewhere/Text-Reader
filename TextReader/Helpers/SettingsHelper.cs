using MetroLog;
using Windows.Storage;

namespace TextReader.Helpers
{
    internal static partial class SettingsHelper
    {
        public static Type Get<Type>(string key) => (Type)LocalSettings.Values[key];

        public static void Set(string key, object value) => LocalSettings.Values[key] = value;

        public static void SetDefaultSettings()
        {
        }
    }

    internal static partial class SettingsHelper
    {
        public static readonly ILogManager LogManager = LogManagerFactory.CreateLogManager();
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        static SettingsHelper() => SetDefaultSettings();
    }
}
