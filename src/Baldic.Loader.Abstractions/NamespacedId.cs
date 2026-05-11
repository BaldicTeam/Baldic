using System;
using System.Text.RegularExpressions;

namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// A stable identifier in the form "namespace:name", e.g. "example:chalk_remote".
    /// Used as primary ID for all registered game objects to avoid collisions between mods.
    /// </summary>
    public readonly struct NamespacedId : IEquatable<NamespacedId>
    {
        private static readonly Regex PartPattern = new Regex(
            @"^[a-z][a-z0-9_]{0,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public string Namespace { get; }
        public string Name { get; }

        public NamespacedId(string @namespace, string name)
        {
            if (!PartPattern.IsMatch(@namespace))
                throw new ArgumentException($"Invalid namespace: '{@namespace}'", nameof(@namespace));
            if (!PartPattern.IsMatch(name))
                throw new ArgumentException($"Invalid name: '{name}'", nameof(name));
            Namespace = @namespace;
            Name = name;
        }

        public static NamespacedId Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"Invalid namespaced id: '{value}'");
            return result;
        }

        public static bool TryParse(string? value, out NamespacedId result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            int sep = value.IndexOf(':');
            if (sep <= 0 || sep == value.Length - 1) return false;
            string ns = value.Substring(0, sep);
            string name = value.Substring(sep + 1);
            if (!PartPattern.IsMatch(ns) || !PartPattern.IsMatch(name)) return false;
            result = new NamespacedId(ns, name);
            return true;
        }

        public bool Equals(NamespacedId other) =>
            string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
            string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is NamespacedId n && Equals(n);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Namespace?.GetHashCode() ?? 0) * 397 ^ (Name?.GetHashCode() ?? 0);
            }
        }

        public override string ToString() => $"{Namespace}:{Name}";

        public static bool operator ==(NamespacedId a, NamespacedId b) => a.Equals(b);
        public static bool operator !=(NamespacedId a, NamespacedId b) => !a.Equals(b);
    }
}
