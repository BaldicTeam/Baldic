using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Manifest
{
    /// <summary>
    /// Result of attempting to parse a <c>baldic.mod.json</c>.
    /// </summary>
    public sealed class ManifestParseResult
    {
        public bool Success { get; }

        /// <summary>Parsed manifest if successful, null otherwise.</summary>
        public ModManifest? Manifest { get; }

        public IReadOnlyList<ParseDiagnostic> Diagnostics { get; }

        public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        public bool HasWarnings => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

        private ManifestParseResult(bool success, ModManifest? manifest, IReadOnlyList<ParseDiagnostic> diagnostics)
        {
            Success = success;
            Manifest = manifest;
            Diagnostics = diagnostics;
        }

        public static ManifestParseResult Ok(ModManifest manifest, IReadOnlyList<ParseDiagnostic>? warnings = null) =>
            new ManifestParseResult(true, manifest, warnings ?? new List<ParseDiagnostic>());

        public static ManifestParseResult Fail(IReadOnlyList<ParseDiagnostic> diagnostics) =>
            new ManifestParseResult(false, null, diagnostics);

        public static ManifestParseResult Fail(string errorCode, string message) =>
            Fail(new[] { ParseDiagnostic.Error(errorCode, message) });
    }
}
