using System.Collections.Generic;
using Newtonsoft.Json;

namespace Baldic.Loader.Abstractions.Manifest
{
    /// <summary>
    /// Declares entrypoint type references for each lifecycle phase.
    /// Each value is a list of fully qualified type names, e.g. "MyMod.Namespace.MyEntrypoint".
    /// </summary>
    public sealed class ManifestEntrypoints
    {
        /// <summary>Main initialization entrypoint — implements <c>IBaldicModInitializer</c>.</summary>
        [JsonProperty("main")]
        public List<string>? Main { get; set; }

        /// <summary>Called after game assets are loaded — implements <c>IBaldicAssetsLoadedEntrypoint</c>.</summary>
        [JsonProperty("assetsLoaded")]
        public List<string>? AssetsLoaded { get; set; }

        /// <summary>Generator registration — implements <c>IBaldicGeneratorEntrypoint</c>.</summary>
        [JsonProperty("generator")]
        public List<string>? Generator { get; set; }

        /// <summary>Options menu registration — implements <c>IBaldicOptionsEntrypoint</c>.</summary>
        [JsonProperty("options")]
        public List<string>? Options { get; set; }

        /// <summary>Pre-launch phase (before Unity init) — implements <c>IBaldicPreLaunchEntrypoint</c>.</summary>
        [JsonProperty("preLaunch")]
        public List<string>? PreLaunch { get; set; }

        /// <summary>Custom entrypoint keys declared by API modules.</summary>
        [JsonExtensionData]
        public Dictionary<string, object?>? Extra { get; set; }
    }
}
