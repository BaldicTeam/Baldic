using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Baldic.Cli.Fingerprint
{
    /// <summary>
    /// Inspects a BB+ installation and generates a <see cref="GameProfile"/> fingerprint.
    /// </summary>
    public static class GameFingerprinter
    {
        public static GameProfile Fingerprint(string gamePath, string? knownVersion)
        {
            if (!Directory.Exists(gamePath))
                throw new DirectoryNotFoundException($"Game directory not found: {gamePath}");

            string managedDir = Path.Combine(gamePath, "BALDI_Data", "Managed");
            if (!Directory.Exists(managedDir))
                throw new DirectoryNotFoundException(
                    $"Managed assemblies not found at '{managedDir}'. Is this a valid BB+ installation?");

            string asmPath = Path.Combine(managedDir, "Assembly-CSharp.dll");
            if (!File.Exists(asmPath))
                throw new FileNotFoundException("Assembly-CSharp.dll not found.", asmPath);

            string hash = Sha256File(asmPath);
            string unityVersion = DetectUnityVersion(Path.Combine(gamePath, "BALDI_Data", "globalgamemanagers"));
            string mvid = GetMvid(asmPath);
            string platform = DetectPlatform();

            var profile = new GameProfile
            {
                Game = "baldis-basics-plus",
                Version = knownVersion ?? "unknown",
                Platform = platform,
                AssemblyCSharpSha256 = hash,
                Mvid = mvid,
                UnityVersion = unityVersion,
            };

            return profile;
        }

        public static void Save(GameProfile profile, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(outputPath, json);
        }

        private static string Sha256File(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            byte[] bytes = sha.ComputeHash(stream);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        private static string DetectUnityVersion(string globalGameManagersPath)
        {
            try
            {
                if (!File.Exists(globalGameManagersPath)) return "unknown";
                byte[] bytes = File.ReadAllBytes(globalGameManagersPath);
                string text = Encoding.ASCII.GetString(bytes);
                var match = Regex.Match(text, @"20\d{2}\.\d+\.\d+\w*");
                return match.Success ? match.Value : "unknown";
            }
            catch { return "unknown"; }
        }

        private static string GetMvid(string asmPath)
        {
            // Read MVID directly from the PE metadata without loading the assembly.
            // MVID is stored in the Module table of the CLI metadata header.
            try
            {
                using var fs = File.OpenRead(asmPath);
                using var peReader = new PEReader(fs);
                MetadataReader metadataReader = peReader.GetMetadataReader();
                ModuleDefinition moduleDefinition = metadataReader.GetModuleDefinition();
                return metadataReader.GetGuid(moduleDefinition.Mvid).ToString();
            }
            catch
            {
                return "unknown";
            }
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
