using MetroLog;
using Windows.System.Profile;

namespace TextReader.Helpers
{
    public static partial class SettingsHelper
    {
        private static readonly ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);

        public static ILogManager LogManager { get; } = LogManagerFactory.CreateLogManager();
        public static ulong OperatingSystemVersion { get; } = (version & 0x00000000FFFF0000L) >> 16;
    }
}
