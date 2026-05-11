using System.Collections.Generic;
using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    /// <summary>
    /// Parsed representation of a <c>baldic.mod.json</c> manifest file.
    /// </summary>
    public sealed class ModManifest
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("authors")]
        public List<ManifestAuthor>? Authors { get; set; }

        [JsonProperty("contributors")]
        public List<ManifestAuthor>? Contributors { get; set; }

        [JsonProperty("license")]
        public string? License { get; set; }

        [JsonProperty("icon")]
        public string? Icon { get; set; }

        [JsonProperty("environment")]
        public string? Environment { get; set; }

        [JsonProperty("game")]
        public ManifestGameCompat? Game { get; set; }

        [JsonProperty("loader")]
        public ManifestLoaderCompat? Loader { get; set; }

        [JsonProperty("depends")]
        public Dictionary<string, string>? Depends { get; set; }

        [JsonProperty("recommends")]
        public Dictionary<string, string>? Recommends { get; set; }

        [JsonProperty("suggests")]
        public Dictionary<string, string>? Suggests { get; set; }

        [JsonProperty("conflicts")]
        public Dictionary<string, string>? Conflicts { get; set; }

        [JsonProperty("breaks")]
        public Dictionary<string, string>? Breaks { get; set; }

        [JsonProperty("provides")]
        public List<string>? Provides { get; set; }

        [JsonProperty("assemblies")]
        public List<string>? Assemblies { get; set; }

        [JsonProperty("entrypoints")]
        public ManifestEntrypoints? Entrypoints { get; set; }

        [JsonProperty("patches")]
        public ManifestPatches? Patches { get; set; }

        [JsonProperty("resources")]
        public ManifestResources? Resources { get; set; }

        [JsonProperty("custom")]
        public Dictionary<string, object>? Custom { get; set; }

        /// <summary>
        /// Resolved semantic version. Populated after manifest parsing succeeds.
        /// </summary>
        [JsonIgnore]
        public SemanticVersion ResolvedVersion { get; set; }

        public override string ToString() => $"{Id} {Version}";
    }
}
