using System;
using System.Collections.Generic;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Resources.Assets
{
    /// <summary>
    /// Per-mod typed asset storage. Ported from MTM101BMDE <c>AssetManager</c>
    /// with stronger namespacing and ownership.
    ///
    /// All keys are namespaced: "modid:assetname".
    /// </summary>
    public sealed class AssetRegistry
    {
        private readonly IModContainer _owner;
        private readonly Dictionary<string, object> _assets = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly object _lock = new object();

        public AssetRegistry(IModContainer owner)
        {
            _owner = owner;
        }

        /// <summary>Register an asset under a namespaced key.</summary>
        public void Add<T>(string key, T asset) where T : class
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            string nsKey = Namespace(key);
            lock (_lock)
            {
                if (_assets.ContainsKey(nsKey))
                    throw new InvalidOperationException(
                        $"Asset '{nsKey}' is already registered by mod '{_owner.Id}'.");
                _assets[nsKey] = asset;
            }
        }

        /// <summary>Try to retrieve a typed asset by key. Returns null if not found or wrong type.</summary>
        public T? Get<T>(string key) where T : class
        {
            lock (_lock)
            {
                string nsKey = Namespace(key);
                return _assets.TryGetValue(nsKey, out var val) ? val as T : null;
            }
        }

        /// <summary>Returns true if the key is registered.</summary>
        public bool Contains(string key)
        {
            lock (_lock) { return _assets.ContainsKey(Namespace(key)); }
        }

        /// <summary>Remove a registered asset. Useful for cleanup between scenes.</summary>
        public bool Remove(string key)
        {
            lock (_lock) { return _assets.Remove(Namespace(key)); }
        }

        private string Namespace(string key) =>
            key.Contains(":") ? key : $"{_owner.Id}:{key}";
    }
}
