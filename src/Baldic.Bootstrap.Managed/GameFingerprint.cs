using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Baldic.Loader.Abstractions;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>Detected game build information.</summary>
    internal sealed class GameFingerprint
    {
        public SemanticVersion GameVersion { get; }
        public string UnityVersion { get; }
        public string AssemblyCSharpSha256 { get; }
        public string Platform { get; }

        private GameFingerprint(SemanticVersion gameVersion, string unityVersion, string hash, string platform)
        {
            GameVersion = gameVersion;
            UnityVersion = unityVersion;
            AssemblyCSharpSha256 = hash;
            Platform = platform;
        }

        /// <summary>
        /// Detect game fingerprint from the game installation at <paramref name="gameRoot"/>.
        /// Falls back to safe defaults if detection fails.
        /// </summary>
        public static GameFingerprint Detect(string gameRoot, IBaldicLog log)
        {
            string asmPath = Path.Combine(gameRoot, "BALDI_Data", "Managed", "Assembly-CSharp.dll");
            string globalGameManagers = Path.Combine(gameRoot, "BALDI_Data", "globalgamemanagers");

            string hash = ComputeFileSha256(asmPath, log);
            string unityVersion = DetectUnityVersion(globalGameManagers, log);
            SemanticVersion gameVersion = DetectGameVersion(gameRoot, log);
            string platform = DetectPlatform();

            return new GameFingerprint(gameVersion, unityVersion, hash, platform);
        }

        private static string ComputeFileSha256(string path, IBaldicLog log)
        {
            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(path);
                byte[] bytes = sha.ComputeHash(stream);
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.AppendFormat("{0:x2}", b);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                log.Warn($"Cannot compute Assembly-CSharp hash: {ex.Message}");
                return "unknown";
            }
        }

        private static string DetectUnityVersion(string globalGameManagersPath, IBaldicLog log)
        {
            // Unity version string is embedded as a null-terminated ASCII string in globalgamemanagers.
            // Pattern: "2020.3.49f1" etc.
            try
            {
                if (!File.Exists(globalGameManagersPath)) return "unknown";
                byte[] bytes = File.ReadAllBytes(globalGameManagersPath);
                string text = Encoding.ASCII.GetString(bytes);
                var match = Regex.Match(text, @"20\d{2}\.\d+\.\d+\w*");
                return match.Success ? match.Value : "unknown";
            }
            catch (Exception ex)
            {
                log.Warn($"Cannot detect Unity version: {ex.Message}");
                return "unknown";
            }
        }

        private static SemanticVersion DetectGameVersion(string gameRoot, IBaldicLog log)
        {
            // BB+ does not ship a version file. We identify the build by Assembly-CSharp hash
            // matched against known fingerprint profiles. For now, use "0.0.0" as placeholder
            // until fingerprint profiles are loaded.
            //
            // Known hashes (populated by baldic doctor):
            // 96ecd01815714f5a841d4af4e5b80e0981d5b9cab5c43483881cc7d33d12aee5 => 0.14.2 linux

            string asmPath = Path.Combine(gameRoot, "BALDI_Data", "Managed", "Assembly-CSharp.dll");
            string hash = ComputeFileSha256(asmPath, log);

            // Check against shipped fingerprint profiles in Baldic/fingerprints/.
            string profilesDir = Path.Combine(gameRoot, "Baldic", "fingerprints");

            if (Directory.Exists(profilesDir))
            {
                foreach (var profilePath in Directory.EnumerateFiles(profilesDir, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(profilePath);
                        if (json.Contains(hash))
                        {
                            // Extract version from filename: "0.14.2-linux.json" => "0.14.2"
                            string fname = Path.GetFileNameWithoutExtension(profilePath);
                            string versionStr = fname.Split('-')[0];
                            if (SemanticVersion.TryParse(versionStr, out var knownVer))
                            {
                                log.Info($"Matched fingerprint profile: {Path.GetFileName(profilePath)}");
                                return knownVer;
                            }
                        }
                    }
                    catch { /* ignore bad profile files */ }
                }
            }

            log.Warn($"Game version unknown for Assembly-CSharp hash {hash}. Reporting as 0.0.0.");
            return new SemanticVersion(0, 0, 0);
        }

        private static string DetectPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT: return "windows-x64";
                case PlatformID.Unix:    return "linux-x64";
                case PlatformID.MacOSX:  return "macos-x64";
                default:                 return "unknown";
            }
        }
    }
}
