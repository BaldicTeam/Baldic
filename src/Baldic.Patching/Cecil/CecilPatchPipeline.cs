using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baldic.Loader.Abstractions;
using Mono.Cecil;

namespace Baldic.Patching.Cecil
{
    /// <summary>
    /// Runs all registered <see cref="IBaldicCecilPatcher"/> instances against
    /// target assemblies and stores the results in the cache.
    ///
    /// This pipeline runs in the pre-load phase, before any target assembly
    /// is loaded into the AppDomain.
    /// On error, the pipeline falls back to the original assembly and records
    /// the failure for diagnostics.
    /// </summary>
    public sealed class CecilPatchPipeline
    {
        private readonly CecilPatchCache _cache;
        private readonly IBaldicLog _log;
        private readonly string _managedDir;

        private readonly List<(IModContainer Mod, IBaldicCecilPatcher Patcher)> _patchers =
            new List<(IModContainer, IBaldicCecilPatcher)>();

        public CecilPatchPipeline(CecilPatchCache cache, string managedDir, IBaldicLog log)
        {
            _cache = cache;
            _managedDir = managedDir;
            _log = log;
        }

        public void Register(IModContainer mod, IBaldicCecilPatcher patcher)
        {
            _patchers.Add((mod, patcher));
            _log.Debug($"[Cecil] Registered patcher '{patcher.GetType().Name}' from mod '{mod.Id}'.");
        }

        /// <summary>
        /// Run patchers for all target assemblies in dependency order.
        /// Returns a map of assembly name → patched path (may equal original path on failure).
        /// </summary>
        public Dictionary<string, string> Run(IBaldicLoader loader)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Group patchers by target assembly.
            var byTarget = new Dictionary<string, List<(IModContainer Mod, IBaldicCecilPatcher Patcher)>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var (mod, patcher) in _patchers)
            {
                foreach (var target in patcher.TargetAssemblies)
                {
                    if (!byTarget.ContainsKey(target))
                        byTarget[target] = new List<(IModContainer, IBaldicCecilPatcher)>();
                    byTarget[target].Add((mod, patcher));
                }
            }

            foreach (var kvp in byTarget)
            {
                string assemblyName = kvp.Key;
                string originalPath = Path.Combine(_managedDir, assemblyName);

                if (!File.Exists(originalPath))
                {
                    _log.Warn($"[Cecil] Target assembly not found: {originalPath}");
                    continue;
                }

                string patchedPath = RunForAssembly(originalPath, kvp.Value, loader);
                result[assemblyName] = patchedPath;

                // Publish to dll_search_path_override dir so Unity Mono finds patched version.
                if (patchedPath != originalPath)
                    _cache.PublishToManagedCache(originalPath, patchedPath);
            }

            return result;
        }

        private string RunForAssembly(
            string originalPath,
            List<(IModContainer Mod, IBaldicCecilPatcher Patcher)> patchers,
            IBaldicLoader loader)
        {
            string assemblyName = Path.GetFileName(originalPath);

            // Build cache key: original hash + all patcher mod ids + versions.
            string descriptor = string.Join("|", patchers.Select(p =>
                $"{p.Mod.Id}@{p.Mod.Version}/{p.Patcher.GetType().FullName}"));
            string cacheKey = CecilPatchCache.ComputeCacheKey(originalPath, descriptor);

            string? cached = _cache.TryGetCached(originalPath, cacheKey);
            if (cached != null)
            {
                _log.Debug($"[Cecil] Cache hit for {assemblyName}: {cached}");
                return cached;
            }

            _log.Info($"[Cecil] Patching {assemblyName} with {patchers.Count} patcher(s)...");

            // Load assembly with Cecil.
            AssemblyDefinition asmDef;
            try
            {
                var readerParams = new ReaderParameters { ReadWrite = false, ReadSymbols = false };
                asmDef = AssemblyDefinition.ReadAssembly(originalPath, readerParams);
            }
            catch (Exception ex)
            {
                _log.Error($"[Cecil] Cannot read {assemblyName}: {ex.Message}. Using original.");
                return originalPath;
            }

            // Apply each patcher in order.
            bool anyError = false;
            foreach (var (mod, patcher) in patchers)
            {
                var context = new CecilPatchContext(mod, loader, _log, originalPath);
                try
                {
                    patcher.Patch(context, asmDef);
                    _log.Info($"[Cecil] Applied '{patcher.GetType().Name}' from '{mod.Id}'.");
                }
                catch (Exception ex)
                {
                    _log.Error($"[Cecil] Patcher '{patcher.GetType().Name}' from '{mod.Id}' failed: {ex.Message}. Aborting Cecil pipeline for {assemblyName}.");
                    anyError = true;
                    break;
                }
            }

            if (anyError)
            {
                asmDef.Dispose();
                return originalPath;
            }

            // Write patched assembly to cache.
            using var ms = new MemoryStream();
            try
            {
                asmDef.Write(ms);
            }
            catch (Exception ex)
            {
                _log.Error($"[Cecil] Cannot write patched {assemblyName}: {ex.Message}. Using original.");
                asmDef.Dispose();
                return originalPath;
            }
            finally
            {
                asmDef.Dispose();
            }

            string outPath = _cache.Write(originalPath, cacheKey, ms.ToArray());
            _log.Info($"[Cecil] Cached patched {assemblyName} → {outPath}");
            return outPath;
        }
    }
}
