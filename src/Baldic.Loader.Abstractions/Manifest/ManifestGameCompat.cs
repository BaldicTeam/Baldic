using System.Collections.Generic;
using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    public sealed class ManifestGameCompat
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Version range expressions, e.g. [">=0.14.0 &lt;0.15.0"].</summary>
        [JsonProperty("versions", Required = Required.Always)]
        public List<string> Versions { get; set; } = new List<string>();
    }

    public sealed class ManifestLoaderCompat
    {
        /// <summary>Loader version range expressions.</summary>
        [JsonProperty("versions")]
        public List<string>? Versions { get; set; }
    }
}
