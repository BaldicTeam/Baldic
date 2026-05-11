using System.Collections.Generic;
using Newtonsoft.Json;

namespace Baldic.Cli.Fingerprint
{
    /// <summary>
    /// JSON fingerprint profile saved by <c>baldic doctor</c>.
    /// Corresponds to the format described in section 6.2 of the architecture spec.
    /// </summary>
    public sealed class GameProfile
    {
        [JsonProperty("game")]
        public string Game { get; set; } = "baldis-basics-plus";

        [JsonProperty("version")]
        public string Version { get; set; } = "unknown";

        [JsonProperty("platform")]
        public string Platform { get; set; } = "unknown";

        [JsonProperty("assemblyCSharpSha256")]
        public string AssemblyCSharpSha256 { get; set; } = string.Empty;

        [JsonProperty("mvid")]
        public string? Mvid { get; set; }

        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; } = "unknown";

        [JsonProperty("symbols")]
        public Dictionary<string, SymbolEntry>? Symbols { get; set; }
    }

    public sealed class SymbolEntry
    {
        [JsonProperty("declaringType")]
        public string DeclaringType { get; set; } = string.Empty;

        [JsonProperty("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        [JsonProperty("fieldType")]
        public string? FieldType { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }
}
