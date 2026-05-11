using System;
using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Runtime implementation of <see cref="IBaldicLoader"/> available to mods after initialization.
    /// </summary>
    internal sealed class BaldicLoaderImpl : IBaldicLoader
    {
        private readonly IReadOnlyList<IModContainer> _allMods;
        private readonly Dictionary<string, IModContainer> _byId;
        private readonly Dictionary<string, List<object>> _entrypoints;

        public SemanticVersion LoaderVersion { get; }
        public IReadOnlyList<IModContainer> AllMods => _allMods;

        public BaldicLoaderImpl(SemanticVersion loaderVersion, IReadOnlyList<IModContainer> allMods)
        {
            LoaderVersion = loaderVersion;
            _allMods = allMods;
            _byId = allMods.ToDictionary(m => m.Id, StringComparer.Ordinal);
            _entrypoints = new Dictionary<string, List<object>>(StringComparer.Ordinal);
        }

        public bool IsModLoaded(string id) => _byId.ContainsKey(id);

        public IModContainer? GetMod(string id) =>
            _byId.TryGetValue(id, out var m) ? m : null;

        /// <summary>
        /// Register an entrypoint instance for a given lifecycle key.
        /// Called by the loader during initialization, not by mods directly.
        /// </summary>
        public void RegisterEntrypoint(string key, IModContainer mod, object instance)
        {
            if (!_entrypoints.TryGetValue(key, out var list))
            {
                list = new List<object>();
                _entrypoints[key] = list;
            }
            list.Add(new EntrypointBox(mod, instance));
        }

        public IReadOnlyList<EntrypointContainer<T>> GetEntrypoints<T>(string key) where T : class
        {
            if (!_entrypoints.TryGetValue(key, out var list))
                return Array.Empty<EntrypointContainer<T>>();

            var result = new List<EntrypointContainer<T>>(list.Count);
            foreach (var box in list)
            {
                if (box is EntrypointBox eb && eb.Instance is T typed)
                    result.Add(new EntrypointContainer<T>(eb.Mod, typed));
            }
            return result;
        }

        private sealed class EntrypointBox
        {
            public IModContainer Mod { get; }
            public object Instance { get; }
            public EntrypointBox(IModContainer mod, object instance) { Mod = mod; Instance = instance; }
        }
    }
}
