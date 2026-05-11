using System;
using System.IO;

namespace Baldic.Cli.Commands
{
    /// <summary>
    /// <c>baldic install-loader --game &lt;path&gt;</c>
    ///
    /// Installs Baldic into the game directory:
    ///   1. Creates Baldic/ directory structure.
    ///   2. Copies core DLLs to Baldic/core/.
    ///   3. Writes doorstop_config.ini.
    ///   4. Copies Doorstop native library (if present alongside CLI).
    ///   5. Writes default baldic.cfg.
    ///
    /// <c>baldic uninstall-loader --game &lt;path&gt;</c>
    ///
    /// Removes Doorstop config and native lib. Does NOT delete Baldic/mods or Baldic/config.
    /// </summary>
    public static class InstallLoaderCommand
    {
        public static int RunInstall(string? gamePath)
        {
            gamePath = ResolveGamePath(gamePath);
            if (gamePath == null) return PrintGamePathError();

            Console.WriteLine($"Installing Baldic into: {gamePath}");

            string baldicRoot   = Path.Combine(gamePath, "Baldic");
            string coreDir      = Path.Combine(baldicRoot, "core");
            string modsDir      = Path.Combine(baldicRoot, "mods");
            string configDir    = Path.Combine(baldicRoot, "config");
            string cacheDir     = Path.Combine(baldicRoot, "cache");
            string logsDir      = Path.Combine(baldicRoot, "logs");
            string disabledDir  = Path.Combine(baldicRoot, "disabled");

            foreach (var dir in new[] { coreDir, modsDir, configDir, cacheDir, logsDir, disabledDir })
                Directory.CreateDirectory(dir);

            // Write doorstop_config.ini (Doorstop v4 format)
            string doorstopPath = Path.Combine(gamePath, "doorstop_config.ini");
            string doorstopCfg =
                "; Baldic — Unity Doorstop v4\n" +
                "[General]\n" +
                "enabled=true\n" +
                "target_assembly=Baldic/core/Baldic.Bootstrap.Managed.dll\n" +
                "redirect_output_log=false\n" +
                "ignore_disable_switch=false\n\n" +
                "[UnityMono]\n" +
                "dll_search_path_override=\n" +
                "debug_enabled=false\n";

            BackupIfExists(doorstopPath);
            File.WriteAllText(doorstopPath, doorstopCfg);
            Console.WriteLine($"  wrote: doorstop_config.ini");

            // Write baldic.cfg
            string baldicCfgPath = Path.Combine(baldicRoot, "baldic.cfg");
            if (!File.Exists(baldicCfgPath))
            {
                File.WriteAllText(baldicCfgPath,
                    "[bootstrap]\n" +
                    "enabled=true\n" +
                    "log_file=Baldic/logs/bootstrap.log\n\n" +
                    "[loader]\n" +
                    "mods_dir=Baldic/mods\n" +
                    "cache_dir=Baldic/cache\n" +
                    "config_dir=Baldic/config\n");
                Console.WriteLine($"  wrote: Baldic/baldic.cfg");
            }

            // Copy Doorstop SO/DLL if present alongside CLI binary.
            TryCopyDoorstop(gamePath);

            // Copy Baldic core DLLs from CLI's own directory.
            TryCopyCoreDlls(coreDir);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Baldic installed successfully.");
            Console.ResetColor();
            Console.WriteLine("Launch the game to verify. Check Baldic/logs/bootstrap.log for output.");
            return 0;
        }

        public static int RunUninstall(string? gamePath)
        {
            gamePath = ResolveGamePath(gamePath);
            if (gamePath == null) return PrintGamePathError();

            Console.WriteLine($"Uninstalling Baldic from: {gamePath}");

            string doorstopPath = Path.Combine(gamePath, "doorstop_config.ini");
            if (File.Exists(doorstopPath))
            {
                string backup = doorstopPath + ".bak";
                if (File.Exists(backup))
                {
                    File.Copy(backup, doorstopPath, overwrite: true);
                    File.Delete(backup);
                    Console.WriteLine("  restored: doorstop_config.ini from backup");
                }
                else
                {
                    File.Delete(doorstopPath);
                    Console.WriteLine("  removed: doorstop_config.ini");
                }
            }

            foreach (var lib in new[] { "libdoorstop.so", "winhttp.dll", "doorstop.dll" })
            {
                string libPath = Path.Combine(gamePath, lib);
                if (File.Exists(libPath + ".bak"))
                {
                    File.Copy(libPath + ".bak", libPath, overwrite: true);
                    File.Delete(libPath + ".bak");
                    Console.WriteLine($"  restored: {lib}");
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Uninstall complete. Mods and config preserved in Baldic/.");
            Console.ResetColor();
            return 0;
        }

        private static void TryCopyCoreDlls(string coreDir)
        {
            string cliDir = AppContext.BaseDirectory;
            string[] coreAssemblies =
            {
                "Baldic.Bootstrap.Managed.dll",
                "Baldic.Loader.dll",
                "Baldic.Loader.Abstractions.dll",
                "Baldic.Patching.dll",
                "Baldic.API.Core.dll",
                "Newtonsoft.Json.dll",
                "0Harmony.dll",
                "Mono.Cecil.dll",
                "System.Reflection.Emit.Lightweight.dll",
                "System.Reflection.Emit.ILGeneration.dll",
            };

            foreach (var dll in coreAssemblies)
            {
                string src = Path.Combine(cliDir, dll);
                if (!File.Exists(src)) continue;
                string dst = Path.Combine(coreDir, dll);
                File.Copy(src, dst, overwrite: true);
                Console.WriteLine($"  copied: {dll} → Baldic/core/");
            }
        }

        private static void TryCopyDoorstop(string gamePath)
        {
            string cliDir = AppContext.BaseDirectory;
            // Linux: libdoorstop.so  Windows: winhttp.dll
            string[] doorstopFiles = { "libdoorstop.so", "winhttp.dll", "doorstop.dll" };
            foreach (var f in doorstopFiles)
            {
                string src = Path.Combine(cliDir, f);
                if (!File.Exists(src)) continue;
                string dst = Path.Combine(gamePath, f);
                BackupIfExists(dst);
                File.Copy(src, dst, overwrite: true);
                Console.WriteLine($"  copied: {f}");
            }
        }

        private static void BackupIfExists(string path)
        {
            if (File.Exists(path) && !File.Exists(path + ".bak"))
                File.Copy(path, path + ".bak");
        }

        private static string? ResolveGamePath(string? explicit_)
        {
            if (!string.IsNullOrWhiteSpace(explicit_) && Directory.Exists(explicit_)) return explicit_;
            string? env = Environment.GetEnvironmentVariable("BALDIC_GAME_PATH");
            if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) return env;
            string steam = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "Steam", "steamapps", "common", "Baldi's Basics Plus");
            if (Directory.Exists(steam)) return steam;
            return null;
        }

        private static int PrintGamePathError()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Game path not found. Use --game <path> or set BALDIC_GAME_PATH.");
            Console.ResetColor();
            return 1;
        }
    }
}
