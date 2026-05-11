using System;
using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Gameplay.DynamicIds
{
    /// <summary>
    /// Thread-safe allocator for dynamic integer enum extensions.
    ///
    /// Port of MTM101BMDE <c>EnumExtensions</c> with two key differences:
    ///   1. Primary identity is a <see cref="NamespacedId"/>, not a raw string.
    ///   2. Allocation is frozen (deterministically ordered) before the game starts;
    ///      registrations after freeze throw. This prevents non-deterministic saves.
    ///
    /// Integer range: starts at 512 to avoid the vanilla range (0–255)
    /// and the MTM101BMDE compat range (256–511).
    ///
    /// Allocation table key: sort by (ownerModId, stableId.ToString()) — alphabetical.
    /// This is deterministic for the same mod set.
    /// </summary>
    public sealed class EnumAllocator<TEnum>
        where TEnum : struct, Enum
    {
        public const int VanillaRangeMax = 255;
        public const int MtmCompatRangeMax = 511;
        public const int BaldicStartOffset = 512;

        private readonly object _lock = new object();
        private readonly List<(NamespacedId Id, string OwnerModId)> _pending = new();
        private Dictionary<NamespacedId, DynamicGameId<TEnum>>? _frozen;
        private bool _isFrozen;

        /// <summary>
        /// Register a namespaced id before freeze.
        /// Duplicate ids (same namespace+name) from the same or different mods are rejected.
        /// </summary>
        public DynamicGameId<TEnum> Register(NamespacedId id, string ownerModId)
        {
            lock (_lock)
            {
                if (_isFrozen)
                    throw new InvalidOperationException(
                        $"Cannot register '{id}' — EnumAllocator<{typeof(TEnum).Name}> is already frozen.");

                if (_pending.Any(p => p.Id == id))
                    throw new ArgumentException(
                        $"Duplicate dynamic id '{id}' registered by '{ownerModId}'.");

                _pending.Add((id, ownerModId));
                // Return a placeholder; actual runtime value assigned at Freeze().
                return new DynamicGameId<TEnum>(id, default);
            }
        }

        /// <summary>
        /// Freeze the allocator. Sorts registrations deterministically and assigns
        /// integer values. Must be called exactly once, before the game starts.
        /// Returns the final id map.
        /// </summary>
        public IReadOnlyDictionary<NamespacedId, DynamicGameId<TEnum>> Freeze()
        {
            lock (_lock)
            {
                if (_isFrozen) return _frozen!;

                // Sort by ownerModId then stableId for determinism.
                var sorted = _pending
                    .OrderBy(p => p.OwnerModId, StringComparer.Ordinal)
                    .ThenBy(p => p.Id.ToString(), StringComparer.Ordinal)
                    .ToList();

                _frozen = new Dictionary<NamespacedId, DynamicGameId<TEnum>>();
                for (int i = 0; i < sorted.Count; i++)
                {
                    int intValue = BaldicStartOffset + i;
                    TEnum enumValue = (TEnum)(object)intValue;
                    var id = new DynamicGameId<TEnum>(sorted[i].Id, enumValue);
                    _frozen[sorted[i].Id] = id;
                }

                _isFrozen = true;
                return _frozen;
            }
        }

        /// <summary>
        /// Look up the frozen <see cref="DynamicGameId{TEnum}"/> for a given stable id.
        /// Returns null if not found or not frozen.
        /// </summary>
        public DynamicGameId<TEnum>? TryGet(NamespacedId id)
        {
            lock (_lock)
            {
                if (_frozen != null && _frozen.TryGetValue(id, out var result))
                    return result;
                return null;
            }
        }

        /// <summary>
        /// Get the stable id corresponding to a runtime integer, or null.
        /// O(n) — only use for diagnostics, not hot path.
        /// </summary>
        public NamespacedId? TryGetStableId(TEnum runtimeValue)
        {
            int target = (int)(object)runtimeValue;
            lock (_lock)
            {
                if (_frozen == null) return null;
                foreach (var kvp in _frozen)
                    if ((int)(object)kvp.Value.RuntimeValue == target) return kvp.Key;
                return null;
            }
        }

        public bool IsFrozen { get { lock (_lock) { return _isFrozen; } } }
        public int Count { get { lock (_lock) { return _pending.Count; } } }
    }
}
