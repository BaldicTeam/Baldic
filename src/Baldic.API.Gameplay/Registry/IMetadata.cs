using System.Collections.Generic;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Gameplay.Registry
{
    /// <summary>
    /// Metadata wrapper for a registered game object (item, NPC, event, etc.).
    /// Provides ownership, tagging, and namespaced identity.
    /// </summary>
    public interface IMetadata<T>
    {
        NamespacedId Id { get; }
        IModContainer Owner { get; }
        T Value { get; }
        ISet<string> Tags { get; }
    }

    /// <summary>
    /// Default concrete metadata implementation.
    /// </summary>
    public sealed class Metadata<T> : IMetadata<T>
    {
        private readonly HashSet<string> _tags;

        public NamespacedId Id { get; }
        public IModContainer Owner { get; }
        public T Value { get; }
        public ISet<string> Tags => _tags;

        public Metadata(NamespacedId id, IModContainer owner, T value, IEnumerable<string>? tags = null)
        {
            Id = id;
            Owner = owner;
            Value = value;
            _tags = tags != null ? new HashSet<string>(tags) : new HashSet<string>();
        }

        public void AddTag(string tag) => _tags.Add(tag);

        public override string ToString() => $"{Id} [{Owner.Id}] tags=[{string.Join(",", _tags)}]";
    }
}
