using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    public sealed class ManifestAuthor
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("contact")]
        public string? Contact { get; set; }
    }
}
