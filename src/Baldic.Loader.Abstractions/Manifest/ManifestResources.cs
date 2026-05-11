using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    public sealed class ManifestResources
    {
        /// <summary>Root directory for all mod assets, relative to package root.</summary>
        [JsonProperty("root")]
        public string? Root { get; set; }

        /// <summary>Localization files directory, relative to package root.</summary>
        [JsonProperty("localization")]
        public string? Localization { get; set; }

        /// <summary>AssetBundles directory, relative to package root.</summary>
        [JsonProperty("assetBundles")]
        public string? AssetBundles { get; set; }
    }
}
