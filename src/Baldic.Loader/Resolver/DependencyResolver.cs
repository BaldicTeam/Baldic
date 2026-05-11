using System;
using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Manifest;

namespace Baldic.Loader.Resolver
{
    /// <summary>
    /// Resolves mod dependencies, detects conflicts, duplicate ids and cycles,
    /// and produces a deterministic load order.
    /// </summary>
    public sealed class DependencyResolver
    {
        private readonly SemanticVersion _loaderVersion;
        private readonly SemanticVersion _gameVersion;
        private readonly IBaldicLog _log;

        public DependencyResolver(SemanticVersion loaderVersion, SemanticVersion gameVersion, IBaldicLog log)
        {
            _loaderVersion = loaderVersion;
            _gameVersion = gameVersion;
            _log = log;
        }

        /// <summary>
        /// Resolve the given set of discovered mods (user mods + built-ins combined).
        /// Built-in mods should be passed in <paramref name="builtIns"/> and are always present.
        /// </summary>
        public ResolverResult Resolve(
            IReadOnlyList<ModManifest> userMods,
            IReadOnlyList<ModManifest> builtIns)
        {
            var problems = new List<ModProblem>();

            // Combine all candidates; build-ins go first so user mods can depend on them.
            var all = new List<ModManifest>(builtIns.Count + userMods.Count);
            all.AddRange(builtIns);
            all.AddRange(userMods);

            // 1. Check for duplicate ids (considering 'provides' aliases)
            var providedIds = BuildProviderMap(all, problems);

            // 2. Check game version compatibility
            foreach (var mod in userMods)
            {
                CheckGameVersionCompat(mod, problems);
                CheckLoaderVersionCompat(mod, problems);
            }

            if (problems.Count > 0) return ResolverResult.Fail(problems);

            // 3. Check hard dependencies ('depends')
            foreach (var mod in all)
            {
                if (mod.Depends == null) continue;
                foreach (var dep in mod.Depends)
                {
                    CheckDependency(mod, dep.Key, dep.Value, providedIds, problems);
                }
            }

            // 4. Check 'breaks'
            foreach (var mod in all)
            {
                if (mod.Breaks == null) continue;
                foreach (var brk in mod.Breaks)
                {
                    if (providedIds.TryGetValue(brk.Key, out var present))
                    {
                        if (VersionRange.TryParse(brk.Value, out var range, out _) && range!.Matches(present.ResolvedVersion))
                        {
                            problems.Add(new ModProblem(ModProblemKind.BreaksViolation, mod.Id,
                                $"'{mod.Id}' breaks '{brk.Key} {brk.Value}' which is present ({present.Version})."));
                        }
                    }
                }
            }

            if (problems.Count > 0) return ResolverResult.Fail(problems);

            // 5. Topological sort (Kahn's algorithm)
            var loadOrder = TopologicalSort(all, providedIds, problems);
            if (problems.Count > 0) return ResolverResult.Fail(problems);

            _log.Info($"Resolved {loadOrder.Count} mods successfully.");
            return ResolverResult.Ok(loadOrder);
        }

        private Dictionary<string, ModManifest> BuildProviderMap(
            List<ModManifest> all,
            List<ModProblem> problems)
        {
            var map = new Dictionary<string, ModManifest>(StringComparer.Ordinal);

            foreach (var mod in all)
            {
                if (map.ContainsKey(mod.Id))
                {
                    problems.Add(new ModProblem(ModProblemKind.DuplicateId, mod.Id,
                        $"Duplicate mod id '{mod.Id}'. Two different mods share the same id."));
                    continue;
                }
                map[mod.Id] = mod;

                if (mod.Provides != null)
                {
                    foreach (var alias in mod.Provides)
                    {
                        if (!map.ContainsKey(alias))
                            map[alias] = mod;
                        // If alias already claimed by a real mod, skip silently — real mod wins.
                    }
                }
            }

            return map;
        }

        private void CheckGameVersionCompat(ModManifest mod, List<ModProblem> problems)
        {
            if (mod.Game == null) return;
            if (mod.Game.Versions == null || mod.Game.Versions.Count == 0) return;

            bool anyMatch = false;
            foreach (var rangeStr in mod.Game.Versions)
            {
                if (VersionRange.TryParse(rangeStr, out var range, out _) && range!.Matches(_gameVersion))
                {
                    anyMatch = true;
                    break;
                }
            }

            if (!anyMatch)
            {
                problems.Add(new ModProblem(ModProblemKind.GameVersionMismatch, mod.Id,
                    $"'{mod.Id}' requires game version {string.Join(" || ", mod.Game.Versions)}, "
                    + $"but current game version is {_gameVersion}."));
            }
        }

        private void CheckLoaderVersionCompat(ModManifest mod, List<ModProblem> problems)
        {
            if (mod.Loader?.Versions == null || mod.Loader.Versions.Count == 0) return;

            bool anyMatch = false;
            foreach (var rangeStr in mod.Loader.Versions)
            {
                if (VersionRange.TryParse(rangeStr, out var range, out _) && range!.Matches(_loaderVersion))
                {
                    anyMatch = true;
                    break;
                }
            }

            if (!anyMatch)
            {
                problems.Add(new ModProblem(ModProblemKind.LoaderVersionMismatch, mod.Id,
                    $"'{mod.Id}' requires loader version {string.Join(" || ", mod.Loader.Versions)}, "
                    + $"but current loader version is {_loaderVersion}."));
            }
        }

        private void CheckDependency(
            ModManifest mod,
            string depId,
            string rangeStr,
            Dictionary<string, ModManifest> map,
            List<ModProblem> problems)
        {
            if (!map.TryGetValue(depId, out var depMod))
            {
                problems.Add(new ModProblem(ModProblemKind.MissingDependency, mod.Id,
                    $"'{mod.Id}' depends on '{depId} {rangeStr}', but '{depId}' is not installed."));
                return;
            }

            if (!VersionRange.TryParse(rangeStr, out var range, out string err))
            {
                problems.Add(new ModProblem(ModProblemKind.MissingDependency, mod.Id,
                    $"'{mod.Id}' has invalid version range for dependency '{depId}': {err}"));
                return;
            }

            if (!range!.Matches(depMod.ResolvedVersion))
            {
                problems.Add(new ModProblem(ModProblemKind.MissingDependency, mod.Id,
                    $"'{mod.Id}' depends on '{depId} {rangeStr}', but installed is {depMod.Version}."));
            }
        }

        private List<ModManifest> TopologicalSort(
            List<ModManifest> all,
            Dictionary<string, ModManifest> map,
            List<ModProblem> problems)
        {
            // Kahn's BFS topological sort.
            var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);
            var edges = new Dictionary<string, List<string>>(StringComparer.Ordinal); // id -> list of ids that depend on it

            foreach (var mod in all)
            {
                if (!inDegree.ContainsKey(mod.Id)) inDegree[mod.Id] = 0;
                if (!edges.ContainsKey(mod.Id)) edges[mod.Id] = new List<string>();
            }

            foreach (var mod in all)
            {
                if (mod.Depends == null) continue;
                foreach (var dep in mod.Depends)
                {
                    if (!map.ContainsKey(dep.Key)) continue; // already reported as missing
                    string resolvedId = map[dep.Key].Id;
                    if (!edges.ContainsKey(resolvedId)) edges[resolvedId] = new List<string>();
                    edges[resolvedId].Add(mod.Id);
                    inDegree[mod.Id] = inDegree.GetValueOrDefault(mod.Id, 0) + 1;
                }
            }

            var queue = new Queue<string>();
            foreach (var kvp in inDegree)
                if (kvp.Value == 0) queue.Enqueue(kvp.Key);

            var sorted = new List<ModManifest>(all.Count);
            var idToMod = all.ToDictionary(m => m.Id, StringComparer.Ordinal);

            while (queue.Count > 0)
            {
                // Sort queue entries for determinism: alphabetical by id
                var next = queue.OrderBy(id => id, StringComparer.Ordinal).First();
                queue = new Queue<string>(queue.Where(id => id != next));

                if (idToMod.TryGetValue(next, out var mod))
                    sorted.Add(mod);

                foreach (var dependent in edges[next])
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                        queue.Enqueue(dependent);
                }
            }

            if (sorted.Count != all.Count)
            {
                // Cycle detected
                var inCycle = all.Select(m => m.Id).Except(sorted.Select(m => m.Id));
                problems.Add(new ModProblem(ModProblemKind.CircularDependency, "multiple",
                    $"Circular dependency detected among: {string.Join(", ", inCycle)}"));
            }

            return sorted;
        }
    }

    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict,
            TKey key,
            TValue defaultValue = default!)
            where TKey : notnull
        {
            return dict.TryGetValue(key, out var val) ? val : defaultValue;
        }
    }
}
