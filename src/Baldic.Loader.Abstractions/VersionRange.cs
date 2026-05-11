using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// A version range expression like ">=1.0.0 &lt;2.0.0", ">=0.1.0", "*", or "1.0.0".
    /// Multiple space-separated comparators are ANDed together.
    /// </summary>
    public sealed class VersionRange
    {
        private static readonly Regex ComparatorPattern = new Regex(
            @"^(?<op>>=|<=|>|<|=|~\^|~|\^|\*)?(?<ver>\*|[0-9].*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly List<Comparator> _comparators;

        private VersionRange(List<Comparator> comparators)
        {
            _comparators = comparators;
        }

        /// <summary>Wildcard — matches any version.</summary>
        public static readonly VersionRange Any = new VersionRange(new List<Comparator>());

        public static VersionRange Parse(string expression)
        {
            if (!TryParse(expression, out var result, out string error))
                throw new FormatException($"Invalid version range '{expression}': {error}");
            return result!;
        }

        public static bool TryParse(string? expression, out VersionRange? result, out string error)
        {
            result = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "Expression is null or empty";
                return false;
            }

            expression = expression.Trim();
            if (expression == "*")
            {
                result = Any;
                return true;
            }

            var parts = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var comparators = new List<Comparator>(parts.Length);

            foreach (var part in parts)
            {
                var m = ComparatorPattern.Match(part);
                if (!m.Success)
                {
                    error = $"Unrecognized comparator: '{part}'";
                    return false;
                }

                string op = m.Groups["op"].Success ? m.Groups["op"].Value : "=";
                string verStr = m.Groups["ver"].Value;

                if (!SemanticVersion.TryParse(verStr, out var ver))
                {
                    error = $"Invalid version in comparator: '{verStr}'";
                    return false;
                }

                comparators.Add(new Comparator(op, ver));
            }

            result = new VersionRange(comparators);
            return true;
        }

        public bool Matches(SemanticVersion version)
        {
            if (_comparators.Count == 0) return true; // wildcard
            foreach (var c in _comparators)
            {
                if (!c.Matches(version)) return false;
            }
            return true;
        }

        public override string ToString()
        {
            if (_comparators.Count == 0) return "*";
            return string.Join(" ", _comparators);
        }

        private readonly struct Comparator
        {
            private readonly string _op;
            private readonly SemanticVersion _version;

            public Comparator(string op, SemanticVersion version)
            {
                _op = op;
                _version = version;
            }

            public bool Matches(SemanticVersion v)
            {
                int cmp = v.CompareTo(_version);
                return _op switch
                {
                    ">=" => cmp >= 0,
                    "<=" => cmp <= 0,
                    ">"  => cmp > 0,
                    "<"  => cmp < 0,
                    "="  => cmp == 0,
                    _    => cmp == 0,
                };
            }

            public override string ToString() => $"{_op}{_version}";
        }
    }
}
