using System.Collections.Generic;

namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// Public facade for querying the running Baldic loader state.
    /// Available to mods after <c>LoaderInitialized</c> lifecycle stage.
    /// </summary>
    public interface IBaldicLoader
    {
        /// <summary>Loader version.</summary>
        SemanticVersion LoaderVersion { get; }

        /// <summary>All resolved and loaded mods, including built-ins.</summary>
        IReadOnlyList<IModContainer> AllMods { get; }

        /// <summary>Returns true if a mod with the given id is in the active mod set.</summary>
        bool IsModLoaded(string id);

        /// <summary>Returns the mod container for the given id, or null.</summary>
        IModContainer? GetMod(string id);

        /// <summary>Returns entrypoint instances for a given lifecycle key and interface type.</summary>
        IReadOnlyList<EntrypointContainer<T>> GetEntrypoints<T>(string key) where T : class;
    }

    /// <summary>
    /// Associates an entrypoint instance with its owning mod container.
    /// </summary>
    public sealed class EntrypointContainer<T> where T : class
    {
        public IModContainer Mod { get; }
        public T Instance { get; }

        public EntrypointContainer(IModContainer mod, T instance)
        {
            Mod = mod;
            Instance = instance;
        }
    }
}
