using System;
using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Gameplay.Registry
{
    /// <summary>
    /// Generic thread-safe metadata storage for any game object type T.
    /// Port of MTM101BMDE item/NPC/event metadata storages, generalised to use
    /// <see cref="NamespacedId"/> as primary key.
    ///
    /// Supports:
    /// - Lookup by namespaced id
    /// - Lookup by value (object reference)
    /// - Query by tag
    /// - Query by owner mod
    /// </summary>
    public sealed class MetadataStorage<T> where T : class
    {
        private readonly object _lock = new object();
        private readonly List<Metadata<T>> _entries = new List<Metadata<T>>();
        private readonly Dictionary<NamespacedId, Metadata<T>> _byId =
            new Dictionary<NamespacedId, Metadata<T>>();

        // ── Registration ──────────────────────────────────────────────────────

        public Metadata<T> Add(NamespacedId id, IModContainer owner, T value, IEnumerable<string>? tags = null)
        {
            lock (_lock)
            {
                if (_byId.ContainsKey(id))
                    throw new ArgumentException(
                        $"Metadata id '{id}' is already registered by mod '{_byId[id].Owner.Id}'.");

                var meta = new Metadata<T>(id, owner, value, tags);
                _entries.Add(meta);
                _byId[id] = meta;
                return meta;
            }
        }

        public bool Remove(NamespacedId id)
        {
            lock (_lock)
            {
                if (!_byId.TryGetValue(id, out var meta)) return false;
                _byId.Remove(id);
                _entries.Remove(meta);
                return true;
            }
        }

        // ── Lookup ────────────────────────────────────────────────────────────

        public IMetadata<T>? TryGet(NamespacedId id)
        {
            lock (_lock) { return _byId.TryGetValue(id, out var m) ? m : null; }
        }

        public IMetadata<T>? TryGetByValue(T value)
        {
            lock (_lock) { return _entries.FirstOrDefault(m => ReferenceEquals(m.Value, value)); }
        }

        public IReadOnlyList<IMetadata<T>> GetAll()
        {
            lock (_lock) { return _entries.Cast<IMetadata<T>>().ToList(); }
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public IReadOnlyList<IMetadata<T>> GetByTag(string tag)
        {
            lock (_lock)
            {
                return _entries.Where(m => m.Tags.Contains(tag))
                    .Cast<IMetadata<T>>()
                    .ToList();
            }
        }

        public IReadOnlyList<IMetadata<T>> GetByOwner(string modId)
        {
            lock (_lock)
            {
                return _entries.Where(m => m.Owner.Id == modId)
                    .Cast<IMetadata<T>>()
                    .ToList();
            }
        }

        public int Count { get { lock (_lock) { return _entries.Count; } } }
    }
}
