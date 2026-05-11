namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// Well-known IDs and constants used throughout the Baldic loader.
    /// </summary>
    public static class LoaderConstants
    {
        public const string LoaderId = "baldic";
        public const string LoaderApiId = "baldic-api";
        public const string GameId = "baldis-basics-plus";

        public const int ManifestSchemaVersion = 1;

        public const string ModsDirectoryName = "mods";
        public const string DisabledDirectoryName = "disabled";
        public const string CacheDirectoryName = "cache";
        public const string ConfigDirectoryName = "config";
        public const string LogsDirectoryName = "logs";
        public const string CoreDirectoryName = "core";
        public const string ReportsDirectoryName = "reports";

        public const string ManifestFileName = "baldic.mod.json";
        public const string ModPackageExtension = ".baldicmod";
        public const string DevLinkExtension = ".baldicdev";
        public const string BootstrapConfigFileName = "baldic.cfg";
        public const string SafeModeFlagFileName = "safe-mode.flag";

        public const string LatestLogFileName = "latest.log";
        public const string BootstrapLogFileName = "bootstrap.log";
    }
}
