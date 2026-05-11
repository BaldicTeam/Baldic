using System;
using System.Text.RegularExpressions;

namespace Baldic.Loader.Abstractions
{
    /// <summary>
    /// An immutable SemVer 2.0 version value.
    /// </summary>
    public readonly struct SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private static readonly Regex Pattern = new Regex(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)"
            + @"(?:-(?<pre>[0-9A-Za-z\-]+(?:\.[0-9A-Za-z\-]+)*))?"
            + @"(?:\+(?<build>[0-9A-Za-z\-]+(?:\.[0-9A-Za-z\-]+)*))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string? PreRelease { get; }
        public string? BuildMetadata { get; }

        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        public SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
            if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));
            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = string.IsNullOrEmpty(preRelease) ? null : preRelease;
            BuildMetadata = string.IsNullOrEmpty(buildMetadata) ? null : buildMetadata;
        }

        public static SemanticVersion Parse(string value)
        {
            if (!TryParse(value, out var result))
                throw new FormatException($"Invalid semantic version: '{value}'");
            return result;
        }

        public static bool TryParse(string? value, out SemanticVersion result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var m = Pattern.Match(value.Trim());
            if (!m.Success) return false;

            int major = int.Parse(m.Groups["major"].Value);
            int minor = int.Parse(m.Groups["minor"].Value);
            int patch = int.Parse(m.Groups["patch"].Value);
            string? pre = m.Groups["pre"].Success ? m.Groups["pre"].Value : null;
            string? build = m.Groups["build"].Success ? m.Groups["build"].Value : null;

            result = new SemanticVersion(major, minor, patch, pre, build);
            return true;
        }

        /// <summary>
        /// Compares two versions. Build metadata is ignored per SemVer 2.0 spec.
        /// Pre-release versions are lower than the release version.
        /// </summary>
        public int CompareTo(SemanticVersion other)
        {
            int cmp = Major.CompareTo(other.Major);
            if (cmp != 0) return cmp;
            cmp = Minor.CompareTo(other.Minor);
            if (cmp != 0) return cmp;
            cmp = Patch.CompareTo(other.Patch);
            if (cmp != 0) return cmp;

            // Both release => equal
            if (PreRelease == null && other.PreRelease == null) return 0;
            // Release > pre-release
            if (PreRelease == null) return 1;
            if (other.PreRelease == null) return -1;

            return ComparePreRelease(PreRelease, other.PreRelease);
        }

        private static int ComparePreRelease(string a, string b)
        {
            var partsA = a.Split('.');
            var partsB = b.Split('.');
            int len = Math.Min(partsA.Length, partsB.Length);
            for (int i = 0; i < len; i++)
            {
                bool isNumA = int.TryParse(partsA[i], out int numA);
                bool isNumB = int.TryParse(partsB[i], out int numB);
                if (isNumA && isNumB)
                {
                    int nc = numA.CompareTo(numB);
                    if (nc != 0) return nc;
                }
                else if (isNumA) return -1;
                else if (isNumB) return 1;
                else
                {
                    int sc = string.Compare(partsA[i], partsB[i], StringComparison.Ordinal);
                    if (sc != 0) return sc;
                }
            }
            return partsA.Length.CompareTo(partsB.Length);
        }

        public bool Equals(SemanticVersion other) =>
            Major == other.Major && Minor == other.Minor && Patch == other.Patch
            && PreRelease == other.PreRelease;

        public override bool Equals(object? obj) => obj is SemanticVersion sv && Equals(sv);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = Major * 397 ^ Minor;
                h = h * 397 ^ Patch;
                h = h * 397 ^ (PreRelease?.GetHashCode() ?? 0);
                return h;
            }
        }

        public override string ToString()
        {
            string s = $"{Major}.{Minor}.{Patch}";
            if (PreRelease != null) s += $"-{PreRelease}";
            if (BuildMetadata != null) s += $"+{BuildMetadata}";
            return s;
        }

        public static bool operator ==(SemanticVersion a, SemanticVersion b) => a.Equals(b);
        public static bool operator !=(SemanticVersion a, SemanticVersion b) => !a.Equals(b);
        public static bool operator <(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) < 0;
        public static bool operator >(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) > 0;
        public static bool operator <=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) <= 0;
        public static bool operator >=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) >= 0;
    }
}
