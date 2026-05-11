using System;
using System.IO;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Resolves Baldic directory paths relative to the game root.
    /// Unity Doorstop sets the working directory to the game root before calling Initialize().
    /// </summary>
    internal static class BootstrapPaths
    {
        /// <summary>
        /// Attempt to detect the game root directory.
        /// Doorstop sets the process working directory to the game folder.
        /// </summary>
        public static string DetectGameRoot()
        {
            // Doorstop v3/v4 sets CWD to the game root.
            string cwd = Directory.GetCurrentDirectory();

            // Sanity check: BB+ Data folder should be present.
            if (Directory.Exists(Path.Combine(cwd, "BALDI_Data")))
                return cwd;

            // Fallback: walk up from executing assembly location.
            string? asmDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (asmDir != null)
            {
                string candidate = asmDir;
                for (int i = 0; i < 4; i++)
                {
                    if (Directory.Exists(Path.Combine(candidate, "BALDI_Data")))
                        return candidate;
                    string? parent = Path.GetDirectoryName(candidate);
                    if (parent == null) break;
                    candidate = parent;
                }
            }

            return cwd; // best effort
        }

        public static string BaldicRoot(string gameRoot) =>
            Path.Combine(gameRoot, "Baldic");

        public static string LogsDir(string gameRoot) =>
            Path.Combine(BaldicRoot(gameRoot), "logs");

        public static string CoreDir(string gameRoot) =>
            Path.Combine(BaldicRoot(gameRoot), "core");

        public static string ConfigFile(string gameRoot) =>
            Path.Combine(BaldicRoot(gameRoot), "baldic.cfg");

        public static string SafeModeFlag(string gameRoot) =>
            Path.Combine(BaldicRoot(gameRoot), "safe-mode.flag");

        public static string ManagedDir(string gameRoot) =>
            Path.Combine(gameRoot, "BALDI_Data", "Managed");
    }
}
