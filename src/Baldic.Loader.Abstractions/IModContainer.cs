using System.Reflection;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// Represents a loaded mod instance with its metadata, assemblies and resolved root path.
    /// </summary>
    public interface IModContainer
    {
        /// <summary>Stable mod ID from <c>baldic.mod.json</c>.</summary>
        string Id { get; }

        /// <summary>Resolved semantic version.</summary>
        SemanticVersion Version { get; }

        /// <summary>Parsed manifest.</summary>
        ModManifest Manifest { get; }

        /// <summary>Absolute path to extracted/exploded mod root directory.</summary>
        string RootPath { get; }

        /// <summary>All managed assemblies loaded for this mod.</summary>
        Assembly[] Assemblies { get; }

        /// <summary>Whether this is a built-in virtual mod (loader, api, game profile).</summary>
        bool IsBuiltIn { get; }
    }
}
