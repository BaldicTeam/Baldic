using System.Collections.Generic;
using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    public sealed class ManifestPatches
    {
        /// <summary>Fully-qualified Harmony patch class names inside the mod's assemblies.</summary>
        [JsonProperty("harmony")]
        public List<string>? Harmony { get; set; }

        /// <summary>Mono.Cecil pre-load patchers.</summary>
        [JsonProperty("cecil")]
        public List<ManifestCecilPatcher>? Cecil { get; set; }
    }

    public sealed class ManifestCecilPatcher
    {
        /// <summary>Relative path to the Cecil patcher assembly inside the package.</summary>
        [JsonProperty("assembly", Required = Required.Always)]
        public string Assembly { get; set; } = string.Empty;

        /// <summary>Target assembly file names to patch, e.g. "Assembly-CSharp.dll".</summary>
        [JsonProperty("targets")]
        public List<string>? Targets { get; set; }
    }
}
