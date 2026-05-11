using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Scanning
{
    /// <summary>
    /// A discovered mod candidate before dependency resolution.
    /// </summary>
    public sealed class ModCandidate
    {
        /// <summary>Absolute path to the archive, directory or dev link file.</summary>
        public string SourcePath { get; }

        public ModCandidateKind Kind { get; }

        /// <summary>Parsed manifest. Null if manifest parsing failed.</summary>
        public ModManifest? Manifest { get; }

        /// <summary>Parse diagnostics, if any.</summary>
        public string? ParseError { get; }

        public bool IsValid => Manifest != null && ParseError == null;

        public ModCandidate(string sourcePath, ModCandidateKind kind, ModManifest manifest)
        {
            SourcePath = sourcePath;
            Kind = kind;
            Manifest = manifest;
            ParseError = null;
        }

        public ModCandidate(string sourcePath, ModCandidateKind kind, string parseError)
        {
            SourcePath = sourcePath;
            Kind = kind;
            Manifest = null;
            ParseError = parseError;
        }

        public override string ToString() =>
            IsValid ? $"{Manifest!.Id} {Manifest.Version} [{Kind}]" : $"INVALID[{SourcePath}]: {ParseError}";
    }
}
