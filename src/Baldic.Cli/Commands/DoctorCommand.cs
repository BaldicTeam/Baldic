using System;
using System.IO;
using Baldic.Cli.Fingerprint;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Logging;
using Baldic.Loader.Scanning;
using Newtonsoft.Json;

namespace Baldic.Cli.Commands
{
    /// <summary>
    /// <c>baldic doctor --game &lt;path&gt; [--version &lt;ver&gt;] [--save-profile &lt;outpath&gt;]</c>
    ///
    /// Inspects a BB+ installation, reports fingerprint, scans installed mods
    /// and outputs a diagnostic report.
    /// </summary>
    public static class DoctorCommand
    {
        public static int Run(string gamePath, string? knownVersion, string? saveProfilePath)
        {
            Console.WriteLine("Baldic Doctor");
            Console.WriteLine(new string('-', 60));

            // --- Game fingerprint ---
            GameProfile profile;
            try
            {
                profile = GameFingerprinter.Fingerprint(gamePath, knownVersion);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine($"Game path     : {gamePath}");
            Console.WriteLine($"Game version  : {profile.Version}");
            Console.WriteLine($"Unity version : {profile.UnityVersion}");
            Console.WriteLine($"Platform      : {profile.Platform}");
            Console.WriteLine($"Assembly hash : {profile.AssemblyCSharpSha256}");
            Console.WriteLine($"MVID          : {profile.Mvid ?? "unknown"}");

            // Check if hash matches a known profile in the fingerprints directory.
            CheckKnownProfile(profile);

            if (saveProfilePath != null)
            {
                GameFingerprinter.Save(profile, saveProfilePath);
                Console.WriteLine($"\nFingerprint saved to: {saveProfilePath}");
            }

            // --- Mods directory ---
            Console.WriteLine();
            Console.WriteLine("Mods:");
            string modsDir = Path.Combine(gamePath, "Baldic", "mods");
            var log = FileLogger.Null;
            var scanner = new ModCandidateScanner(log);
            var candidates = scanner.Scan(modsDir);

            if (candidates.Count == 0)
            {
                Console.WriteLine("  (no mods found)");
            }
            else
            {
                foreach (var c in candidates)
                {
                    if (c.IsValid)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("  [OK] ");
                        Console.ResetColor();
                        Console.WriteLine($"{c.Manifest!.Id} {c.Manifest.Version}  [{c.Kind}]");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("  [WARN] ");
                        Console.ResetColor();
                        Console.WriteLine($"{c.SourcePath}: {c.ParseError}");
                    }
                }
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Doctor check complete.");
            Console.ResetColor();
            return 0;
        }

        private static void CheckKnownProfile(GameProfile profile)
        {
            // Walk up from the CLI binary to find a fingerprints directory.
            string? dir = System.AppContext.BaseDirectory;
            for (int i = 0; i < 7 && dir != null; i++)
            {
                string fp = Path.Combine(dir, "fingerprints");
                if (Directory.Exists(fp))
                {
                    foreach (var f in Directory.EnumerateFiles(fp, "*.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            string json = File.ReadAllText(f);
                            if (json.Contains(profile.AssemblyCSharpSha256))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Known profile : {Path.GetFileName(f)}");
                                Console.ResetColor();
                                return;
                            }
                        }
                        catch { }
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Known profile : NONE — this build is not in the fingerprints database.");
                    Console.ResetColor();
                    return;
                }
                dir = Path.GetDirectoryName(dir);
            }
            Console.WriteLine("Known profile : (fingerprints directory not found)");
        }
    }
}
