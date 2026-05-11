using System.Reflection;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Concrete mod container created after assembly loading.
    /// </summary>
    internal sealed class ModContainer : IModContainer
    {
        public string Id { get; }
        public SemanticVersion Version { get; }
        public ModManifest Manifest { get; }
        public string RootPath { get; }
        public Assembly[] Assemblies { get; }
        public bool IsBuiltIn { get; }

        public ModContainer(
            ModManifest manifest,
            string rootPath,
            Assembly[] assemblies,
            bool isBuiltIn = false)
        {
            Manifest = manifest;
            Id = manifest.Id;
            Version = manifest.ResolvedVersion.Major != 0 || manifest.ResolvedVersion.Minor != 0 || manifest.ResolvedVersion.Patch != 0
                ? manifest.ResolvedVersion
                : SemanticVersion.Parse(manifest.Version);
            RootPath = rootPath;
            Assemblies = assemblies;
            IsBuiltIn = isBuiltIn;
        }

        public override string ToString() => $"{Id} {Version}";
    }
}
