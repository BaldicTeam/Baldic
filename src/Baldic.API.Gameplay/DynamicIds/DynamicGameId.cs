using System;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Gameplay.DynamicIds
{
    /// <summary>
    /// Associates a stable <see cref="NamespacedId"/> with a runtime integer enum value.
    ///
    /// The integer is allocated deterministically by <see cref="EnumAllocator{TEnum}"/>
    /// based on the mod dependency order, mod id, and registration order — NOT by
    /// the order mods happen to load. This means the same mod set always produces
    /// the same int values, making saves reproducible.
    ///
    /// Save files must store the <see cref="StableId"/>, never the raw int.
    /// </summary>
    public readonly struct DynamicGameId<TEnum> : IEquatable<DynamicGameId<TEnum>>
        where TEnum : struct, Enum
    {
        /// <summary>Persistent namespaced identifier, safe to store in save files.</summary>
        public NamespacedId StableId { get; }

        /// <summary>Runtime integer enum value. Valid only for this game session.</summary>
        public TEnum RuntimeValue { get; }

        public DynamicGameId(NamespacedId stableId, TEnum runtimeValue)
        {
            StableId = stableId;
            RuntimeValue = runtimeValue;
        }

        public bool Equals(DynamicGameId<TEnum> other) =>
            StableId == other.StableId;

        public override bool Equals(object? obj) =>
            obj is DynamicGameId<TEnum> d && Equals(d);

        public override int GetHashCode() => StableId.GetHashCode();

        public override string ToString() =>
            $"{StableId} (runtime={RuntimeValue})";

        public static bool operator ==(DynamicGameId<TEnum> a, DynamicGameId<TEnum> b) => a.Equals(b);
        public static bool operator !=(DynamicGameId<TEnum> a, DynamicGameId<TEnum> b) => !a.Equals(b);
    }
}
