using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Manifest;

namespace Baldic.Loader.Scanning
{
    /// <summary>
    /// Scans a mods directory for mod candidates: packed archives, exploded directories
    /// and dev links.
    /// </summary>
    public sealed class ModCandidateScanner
    {
        private readonly IBaldicLog _log;

        public ModCandidateScanner(IBaldicLog log)
        {
            _log = log;
        }

        /// <summary>
        /// Scan the given directory and return all discovered mod candidates.
        /// Invalid candidates (bad manifest, unsupported schema, etc.) are included
        /// with error information for diagnostic reporting.
        /// </summary>
        public IReadOnlyList<ModCandidate> Scan(string modsDirectory)
        {
            var results = new List<ModCandidate>();

            if (!Directory.Exists(modsDirectory))
            {
                _log.Debug($"Mods directory not found, skipping scan: {modsDirectory}");
                return results;
            }

            // 1. Packed archives: *.baldicmod
            foreach (var file in Directory.EnumerateFiles(modsDirectory, "*" + LoaderConstants.ModPackageExtension))
            {
                var candidate = ScanArchive(file);
                results.Add(candidate);
                _log.Debug($"Found archive: {candidate}");
            }

            // 2. Exploded directories: subdirs containing baldic.mod.json
            foreach (var dir in Directory.EnumerateDirectories(modsDirectory))
            {
                string manifestPath = Path.Combine(dir, LoaderConstants.ManifestFileName);
                if (File.Exists(manifestPath))
                {
                    var candidate = ScanExplodedDirectory(dir, manifestPath);
                    results.Add(candidate);
                    _log.Debug($"Found exploded mod: {candidate}");
                }
            }

            // 3. Dev links: *.baldicdev
            foreach (var file in Directory.EnumerateFiles(modsDirectory, "*" + LoaderConstants.DevLinkExtension))
            {
                var candidate = ScanDevLink(file);
                results.Add(candidate);
                _log.Debug($"Found dev link: {candidate}");
            }

            return results;
        }

        private ModCandidate ScanArchive(string archivePath)
        {
            try
            {
                using var zip = ZipFile.OpenRead(archivePath);
                var entry = zip.GetEntry(LoaderConstants.ManifestFileName);
                if (entry == null)
                    return new ModCandidate(archivePath, ModCandidateKind.PackedArchive,
                        $"Archive missing '{LoaderConstants.ManifestFileName}'");

                string json;
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                    json = reader.ReadToEnd();

                var result = ManifestParser.ParseJson(json);
                if (!result.Success)
                    return new ModCandidate(archivePath, ModCandidateKind.PackedArchive,
                        FormatDiagnostics(result));

                return new ModCandidate(archivePath, ModCandidateKind.PackedArchive, result.Manifest!);
            }
            catch (Exception ex)
            {
                return new ModCandidate(archivePath, ModCandidateKind.PackedArchive,
                    $"Failed to open archive: {ex.Message}");
            }
        }

        private ModCandidate ScanExplodedDirectory(string directory, string manifestPath)
        {
            var result = ManifestParser.ParseFile(manifestPath);
            if (!result.Success)
                return new ModCandidate(directory, ModCandidateKind.ExplodedDirectory,
                    FormatDiagnostics(result));
            return new ModCandidate(directory, ModCandidateKind.ExplodedDirectory, result.Manifest!);
        }

        private ModCandidate ScanDevLink(string devLinkPath)
        {
            try
            {
                string targetPath = File.ReadAllText(devLinkPath).Trim();
                if (!Path.IsPathRooted(targetPath))
                    targetPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(devLinkPath)!, targetPath));

                string manifestPath = Path.Combine(targetPath, LoaderConstants.ManifestFileName);
                if (!File.Exists(manifestPath))
                    return new ModCandidate(devLinkPath, ModCandidateKind.DevLink,
                        $"Dev link target '{targetPath}' does not contain '{LoaderConstants.ManifestFileName}'");

                var result = ManifestParser.ParseFile(manifestPath);
                if (!result.Success)
                    return new ModCandidate(devLinkPath, ModCandidateKind.DevLink,
                        FormatDiagnostics(result));

                return new ModCandidate(targetPath, ModCandidateKind.DevLink, result.Manifest!);
            }
            catch (Exception ex)
            {
                return new ModCandidate(devLinkPath, ModCandidateKind.DevLink,
                    $"Failed to read dev link: {ex.Message}");
            }
        }

        private static string FormatDiagnostics(Manifest.ManifestParseResult result)
        {
            var errors = new System.Text.StringBuilder();
            foreach (var d in result.Diagnostics)
                errors.AppendLine(d.ToString());
            return errors.ToString().TrimEnd();
        }
    }
}
