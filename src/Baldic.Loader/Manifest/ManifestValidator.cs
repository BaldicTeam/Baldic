using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Manifest
{
    /// <summary>
    /// Validates a parsed <see cref="ModManifest"/> and resolves semantic versions.
    /// </summary>
    public static class ManifestValidator
    {
        private static readonly Regex IdPattern = new Regex(
            @"^[a-z][a-z0-9_]{1,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static ManifestParseResult Validate(ModManifest manifest)
        {
            var diagnostics = new List<ParseDiagnostic>();

            // schemaVersion
            if (manifest.SchemaVersion != LoaderConstants.ManifestSchemaVersion)
            {
                diagnostics.Add(ParseDiagnostic.Error("BALDIC_V001",
                    $"Unsupported schemaVersion {manifest.SchemaVersion}. Expected {LoaderConstants.ManifestSchemaVersion}."));
            }

            // id
            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                diagnostics.Add(ParseDiagnostic.Error("BALDIC_V002", "Field 'id' is required."));
            }
            else if (!IdPattern.IsMatch(manifest.Id))
            {
                diagnostics.Add(ParseDiagnostic.Error("BALDIC_V003",
                    $"Invalid mod id '{manifest.Id}'. Must match ^[a-z][a-z0-9_]{{1,63}}$."));
            }

            // version
            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                diagnostics.Add(ParseDiagnostic.Error("BALDIC_V004", "Field 'version' is required."));
            }
            else if (!SemanticVersion.TryParse(manifest.Version, out var semVer))
            {
                diagnostics.Add(ParseDiagnostic.Error("BALDIC_V005",
                    $"Field 'version' is not a valid SemVer string: '{manifest.Version}'."));
            }
            else
            {
                manifest.ResolvedVersion = semVer;
            }

            // game
            if (manifest.Game == null)
            {
                diagnostics.Add(ParseDiagnostic.Warning("BALDIC_V006",
                    "Field 'game' is missing. Mod may not be matched against game versions."));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(manifest.Game.Id))
                    diagnostics.Add(ParseDiagnostic.Error("BALDIC_V007", "Field 'game.id' is required."));

                if (manifest.Game.Versions == null || manifest.Game.Versions.Count == 0)
                    diagnostics.Add(ParseDiagnostic.Warning("BALDIC_V008",
                        "Field 'game.versions' is empty. Mod will be treated as compatible with all game versions."));
                else
                {
                    foreach (var vr in manifest.Game.Versions)
                    {
                        if (!VersionRange.TryParse(vr, out _, out string err))
                        {
                            diagnostics.Add(ParseDiagnostic.Error("BALDIC_V009",
                                $"Invalid version range in 'game.versions': '{vr}': {err}"));
                        }
                    }
                }
            }

            // assemblies path safety
            if (manifest.Assemblies != null)
            {
                foreach (var asm in manifest.Assemblies)
                {
                    if (IsUnsafePath(asm))
                        diagnostics.Add(ParseDiagnostic.Error("BALDIC_V020",
                            $"Assembly path '{asm}' contains path traversal or is absolute. Rejected."));
                }
            }

            // dependency version ranges
            ValidateDependencyMap(manifest.Depends, "depends", diagnostics);
            ValidateDependencyMap(manifest.Breaks, "breaks", diagnostics);
            ValidateDependencyMap(manifest.Conflicts, "conflicts", diagnostics);
            ValidateDependencyMap(manifest.Recommends, "recommends", diagnostics);

            if (diagnostics.Count > 0)
            {
                bool hasErrors = diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);
                if (hasErrors) return ManifestParseResult.Fail(diagnostics);
                return ManifestParseResult.Ok(manifest, diagnostics);
            }

            return ManifestParseResult.Ok(manifest);
        }

        private static void ValidateDependencyMap(
            Dictionary<string, string>? map,
            string fieldName,
            List<ParseDiagnostic> diagnostics)
        {
            if (map == null) return;
            foreach (var kvp in map)
            {
                if (!VersionRange.TryParse(kvp.Value, out _, out string err))
                {
                    diagnostics.Add(ParseDiagnostic.Error("BALDIC_V015",
                        $"Invalid version range in '{fieldName}.{kvp.Key}': '{kvp.Value}': {err}"));
                }
            }
        }

        private static bool IsUnsafePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            if (Path.IsPathRooted(path)) return true;
            if (path.Contains("..")) return true;
            if (path.IndexOf('\0') >= 0) return true;
            return false;
        }
    }
}
