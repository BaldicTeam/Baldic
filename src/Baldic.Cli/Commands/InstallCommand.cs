using System;
using System.IO;
using Baldic.Loader.Abstractions;

namespace Baldic.Cli.Commands
{
    /// <summary>
    /// <c>baldic install [--game &lt;path&gt;] [--mod &lt;path&gt;]</c>
    ///
    /// Copies a .baldicmod file to the game's Baldic/mods directory.
    /// If --mod is not specified, searches the current directory's output folder.
    /// </summary>
    public static class InstallCommand
    {
        public static int Run(string? gamePath, string? modPath)
        {
            gamePath = ResolveGamePath(gamePath);
            if (gamePath == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Game path not found. Use --game <path> or set BALDIC_GAME_PATH.");
                Console.ResetColor();
                return 1;
            }

            string modsDir = Path.Combine(gamePath, "Baldic", LoaderConstants.ModsDirectoryName);

            if (modPath == null)
            {
                modPath = FindModInCurrentDirectory();
                if (modPath == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: No .baldicmod file found. Build your mod first, or specify --mod <path>.");
                    Console.ResetColor();
                    return 1;
                }
            }

            if (!File.Exists(modPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Mod file not found: {modPath}");
                Console.ResetColor();
                return 1;
            }

            Directory.CreateDirectory(modsDir);
            string dest = Path.Combine(modsDir, Path.GetFileName(modPath));
            File.Copy(modPath, dest, overwrite: true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Installed: {Path.GetFileName(modPath)} → {dest}");
            Console.ResetColor();
            return 0;
        }

        private static string? FindModInCurrentDirectory()
        {
            // Search bin/Debug/netstandard2.0/ and current dir.
            var searchDirs = new[]
            {
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "netstandard2.0"),
                Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release", "netstandard2.0"),
            };

            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*" + LoaderConstants.ModPackageExtension);
                if (files.Length > 0) return files[0];
            }
            return null;
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
    }
}
