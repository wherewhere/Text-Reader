using MetroLog;

namespace TextReader.Helpers
{
    public static partial class SettingsHelper
    {
        public static ILogManager LogManager { get; } = LogManagerFactory.CreateLogManager();
    }
}
