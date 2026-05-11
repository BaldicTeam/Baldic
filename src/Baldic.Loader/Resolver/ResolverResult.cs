using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Resolver
{
    /// <summary>Result of dependency resolution.</summary>
    public sealed class ResolverResult
    {
        public bool Success { get; }

        /// <summary>Ordered list of manifests to load, dependencies before dependents.</summary>
        public IReadOnlyList<ModManifest> LoadOrder { get; }

        public IReadOnlyList<ModProblem> Problems { get; }

        public bool HasErrors => Problems.Any(p =>
            p.Kind != ModProblemKind.BreaksViolation); // breaks are user-resolvable warnings treated as errors

        private ResolverResult(bool success, IReadOnlyList<ModManifest> loadOrder, IReadOnlyList<ModProblem> problems)
        {
            Success = success;
            LoadOrder = loadOrder;
            Problems = problems;
        }

        public static ResolverResult Ok(IReadOnlyList<ModManifest> loadOrder) =>
            new ResolverResult(true, loadOrder, new List<ModProblem>());

        public static ResolverResult Fail(IReadOnlyList<ModProblem> problems) =>
            new ResolverResult(false, new List<ModManifest>(), problems);

        public string FormatProblems()
        {
            if (Problems.Count == 0) return string.Empty;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Cannot launch with current mod set:");
            foreach (var p in Problems)
                sb.AppendLine($"- {p}");
            return sb.ToString().TrimEnd();
        }
    }
}
