using System.Collections.Generic;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Creates virtual built-in mod manifests that are always present.
    /// These represent the loader itself, api, and the game profile.
    /// </summary>
    internal static class BuiltInMods
    {
        public static List<ModManifest> Create(SemanticVersion loaderVersion, SemanticVersion gameVersion)
        {
            return new List<ModManifest>
            {
                BuildVirtual(LoaderConstants.LoaderId, loaderVersion, "Baldic Mod Loader"),
                BuildVirtual(LoaderConstants.LoaderApiId, loaderVersion, "Baldic API"),
                BuildGame(gameVersion),
            };
        }

        private static ModManifest BuildVirtual(string id, SemanticVersion version, string name) =>
            new ModManifest
            {
                SchemaVersion = LoaderConstants.ManifestSchemaVersion,
                Id = id,
                Version = version.ToString(),
                Name = name,
                ResolvedVersion = version,
            };

        private static ModManifest BuildGame(SemanticVersion gameVersion) =>
            new ModManifest
            {
                SchemaVersion = LoaderConstants.ManifestSchemaVersion,
                Id = LoaderConstants.GameId,
                Version = gameVersion.ToString(),
                Name = "Baldi's Basics Plus",
                ResolvedVersion = gameVersion,
            };
    }
}
