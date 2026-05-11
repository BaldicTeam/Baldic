using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Entrypoints;
using Baldic.Loader.Abstractions.Manifest;
using Baldic.Loader.Logging;
using Baldic.Loader.Manifest;
using Baldic.Loader.Resolver;
using Baldic.Loader.Scanning;
using Baldic.Patching;
using Baldic.Patching.Cecil;
using HarmonyLib;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Orchestrates mod discovery and resolution phases.
    /// Kept in a separate class to defer Baldic.Loader type loading until after AssemblyResolve
    /// is installed.
    /// </summary>
    internal sealed class LoaderRunner
    {
        private readonly string _gameRoot;
        private readonly BootstrapConfig _config;
        private readonly IBaldicLog _log;

        public LoaderRunner(string gameRoot, BootstrapConfig config, IBaldicLog log)
        {
            _gameRoot = gameRoot;
            _config = config;
            _log = log;
        }

        public void Run()
        {
            _log.Info("--- Baldic Loader starting ---");

            // Read game fingerprint for version detection.
            string fingerprintDir = Path.Combine(BootstrapPaths.BaldicRoot(_gameRoot), "fingerprints");
            var fingerprint = GameFingerprint.Detect(_gameRoot, _log);
            _log.Info($"Game version  : {fingerprint.GameVersion}");
            _log.Info($"Unity version : {fingerprint.UnityVersion}");
            _log.Info($"Assembly hash : {fingerprint.AssemblyCSharpSha256}");

            var loaderVersion = SemanticVersion.Parse("0.1.0");
            var gameVersion = fingerprint.GameVersion;

            // Scan mods directory.
            string modsDir = Path.IsPathRooted(_config.ModsDir)
                ? _config.ModsDir
                : Path.Combine(_gameRoot, _config.ModsDir);

            var scanner = new ModCandidateScanner(_log);
            var candidates = scanner.Scan(modsDir);

            _log.Info($"Discovered {candidates.Count} mod candidate(s).");

            // Collect valid manifests; report invalid ones.
            var manifests = new System.Collections.Generic.List<ModManifest>();
            foreach (var candidate in candidates)
            {
                if (candidate.IsValid)
                    manifests.Add(candidate.Manifest!);
                else
                    _log.Warn($"Skipping invalid mod at '{candidate.SourcePath}': {candidate.ParseError}");
            }

            // Built-in virtual mods.
            var builtIns = BuiltInMods.Create(loaderVersion, gameVersion);

            // Resolve dependencies.
            var resolver = new DependencyResolver(loaderVersion, gameVersion, _log);
            var result = resolver.Resolve(manifests, builtIns);

            if (!result.Success)
            {
                _log.Fatal("Mod resolution failed:\n" + result.FormatProblems());
                return;
            }

            _log.Info($"Load order ({result.LoadOrder.Count} mods):");
            for (int i = 0; i < result.LoadOrder.Count; i++)
                _log.Info($"  [{i + 1:D2}] {result.LoadOrder[i]}");

            // Phase 2: Load mod assemblies.
            var modContainers = LoadModAssemblies(result.LoadOrder, candidates, modsDir);

            // Phase 3: Build IBaldicLoader and invoke OnInitialize entrypoints.
            // OnInitialize explicitly forbids Unity API calls, so it is safe here.
            var allMods = new List<IModContainer>(modContainers);
            var loaderImpl = new BaldicLoaderImpl(loaderVersion, allMods);

            // Phase 4a: Run Cecil IL patches (pre-Unity, writes patched assemblies to cache).
            RunCecilPipeline(modContainers, loaderImpl);

            // Phase 4b: Apply Harmony runtime patches (shared instance).
            var coreHarmony = new Harmony("baldic.core");
            ApplyHarmonyPatches(modContainers, loaderImpl, coreHarmony);

            InvokeMainEntrypoints(loaderImpl, modContainers);

            // Phase 5: Invoke preLaunch + generator + install Unity hook.
            UnityHookRunner.Apply(_log, loaderImpl, modContainers);

            _log.Info("--- Baldic loader initialized ---");
        }

        // ------------------------------------------------------------------ Cecil pipeline

        private void RunCecilPipeline(List<ModContainer> mods, BaldicLoaderImpl loader)
        {
            bool anyPatchers = false;
            foreach (var mod in mods)
                if (mod.Manifest.Patches?.Cecil != null && mod.Manifest.Patches.Cecil.Count > 0)
                { anyPatchers = true; break; }

            if (!anyPatchers) return;

            string cacheRoot = Path.Combine(BootstrapPaths.BaldicRoot(_gameRoot), "cache");
            string managedDir = BootstrapPaths.ManagedDir(_gameRoot);
            var cache    = new CecilPatchCache(cacheRoot);
            var pipeline = new CecilPatchPipeline(cache, managedDir, _log);

            foreach (var mod in mods)
            {
                if (mod.Manifest.Patches?.Cecil == null) continue;
                foreach (var cecilEntry in mod.Manifest.Patches.Cecil)
                {
                    // Load the Cecil patcher assembly from the mod package.
                    string patcherAsmPath = Path.Combine(cacheRoot,
                        mod.Manifest.Id + "-" + mod.Manifest.Version,
                        Path.GetFileName(cecilEntry.Assembly));

                    if (!File.Exists(patcherAsmPath))
                    {
                        _log.Warn($"[Cecil] Patcher assembly not found: {patcherAsmPath}");
                        continue;
                    }

                    System.Reflection.Assembly patcherAsm;
                    try { patcherAsm = System.Reflection.Assembly.LoadFrom(patcherAsmPath); }
                    catch (Exception ex) { _log.Error($"[Cecil] Load patcher failed: {ex.Message}"); continue; }

                    foreach (var type in patcherAsm.GetTypes())
                    {
                        if (!typeof(IBaldicCecilPatcher).IsAssignableFrom(type) || type.IsAbstract) continue;
                        try
                        {
                            var patcher = (IBaldicCecilPatcher)Activator.CreateInstance(type)!;
                            pipeline.Register(mod, patcher);
                        }
                        catch (Exception ex) { _log.Error($"[Cecil] Instantiate {type.Name} failed: {ex.Message}"); }
                    }
                }
            }

            var patchedPaths = pipeline.Run(loader);
            foreach (var kvp in patchedPaths)
                _log.Info($"[Cecil] {kvp.Key} → {kvp.Value}");
        }

        // ------------------------------------------------------------------ harmony patching

        private void ApplyHarmonyPatches(List<ModContainer> mods, BaldicLoaderImpl loader, Harmony coreHarmony)
        {
            // Warm up HarmonySharedState with a no-op patch to avoid init failures later.
            _ = coreHarmony.Id;

            string configRoot = Path.Combine(BootstrapPaths.BaldicRoot(_gameRoot), "config");

            foreach (var mod in mods)
            {
                if (mod.Assemblies.Length == 0) continue;

                var patcher = new HarmonyPatcher(mod, loader, _log, configRoot);

                try
                {
                    var harmonyTypes = mod.Manifest.Patches?.Harmony;
                    if (harmonyTypes != null && harmonyTypes.Count > 0)
                    {
                        patcher.PatchTypes(harmonyTypes, mod.Assemblies);
                        _log.Info($"[Patching] {mod.Id}: {harmonyTypes.Count} explicit type(s) patched.");
                    }
                    else
                    {
                        foreach (var asm in mod.Assemblies)
                            patcher.PatchAll(asm);

                        _log.Debug($"[Patching] {mod.Id}: PatchAll complete.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"[Patching] {mod.Id} Harmony setup failed: {ex.Message}");
                }
            }
        }

        // ------------------------------------------------------------------ assembly loading

        private List<ModContainer> LoadModAssemblies(
            IReadOnlyList<ModManifest> loadOrder,
            IReadOnlyList<ModCandidate> candidates,
            string modsDir)
        {
            // Build lookup: mod id → candidate source path
            var sourceByid = new Dictionary<string, ModCandidate>(StringComparer.Ordinal);
            foreach (var c in candidates)
                if (c.IsValid)
                    sourceByid[c.Manifest!.Id] = c;

            var result = new List<ModContainer>(loadOrder.Count);
            foreach (var manifest in loadOrder)
            {
                // Skip built-in virtual mods (no assemblies).
                if (!sourceByid.TryGetValue(manifest.Id, out var candidate))
                    continue;

                Assembly[] assemblies = LoadAssembliesForMod(manifest, candidate);
                string rootPath = candidate.Kind == ModCandidateKind.PackedArchive
                    ? candidate.SourcePath   // zip path used as identity
                    : candidate.SourcePath;  // directory path

                result.Add(new ModContainer(manifest, rootPath, assemblies));
                _log.Info($"Loaded: {manifest.Id} {manifest.Version} ({assemblies.Length} assemblies)");
            }
            return result;
        }

        private Assembly[] LoadAssembliesForMod(ModManifest manifest, ModCandidate candidate)
        {
            var loaded = new List<Assembly>();

            if (candidate.Kind == ModCandidateKind.PackedArchive)
            {
                // Extract each declared assembly from the zip into a temp cache dir.
                string cacheDir = Path.Combine(
                    BootstrapPaths.BaldicRoot(_gameRoot), "cache",
                    manifest.Id + "-" + manifest.Version);
                Directory.CreateDirectory(cacheDir);

                var asmList = manifest.Assemblies != null && manifest.Assemblies.Count > 0
                    ? manifest.Assemblies
                    : new System.Collections.Generic.List<string> { $"lib/{manifest.Id}.dll" };
                using (var zip = ZipFile.OpenRead(candidate.SourcePath))
                {
                    foreach (string asmRelPath in asmList)
                    {
                        string entryName = asmRelPath.Replace('\\', '/');
                        var entry = zip.GetEntry(entryName);
                        if (entry == null)
                        {
                            _log.Warn($"  [{manifest.Id}] Assembly not found in archive: {entryName}");
                            continue;
                        }

                        string dest = Path.Combine(cacheDir, Path.GetFileName(entryName));
                        if (!File.Exists(dest) || new FileInfo(dest).Length != entry.Length)
                            entry.ExtractToFile(dest, overwrite: true);

                        try
                        {
                            Assembly asm = Assembly.LoadFrom(dest);
                            loaded.Add(asm);
                            _log.Debug($"  [{manifest.Id}] Loaded assembly: {Path.GetFileName(dest)}");
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"  [{manifest.Id}] Failed to load {dest}: {ex.Message}");
                        }
                    }
                }
            }
            else if (candidate.Kind == ModCandidateKind.ExplodedDirectory)
            {
                var asmList2 = manifest.Assemblies != null && manifest.Assemblies.Count > 0
                    ? manifest.Assemblies
                    : new System.Collections.Generic.List<string> { $"lib/{manifest.Id}.dll" };
                foreach (string rel in asmList2)
                {
                    string full = Path.Combine(candidate.SourcePath, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(full)) { _log.Warn($"  [{manifest.Id}] Missing: {full}"); continue; }
                    try
                    {
                        Assembly asm = Assembly.LoadFrom(full);
                        loaded.Add(asm);
                        _log.Debug($"  [{manifest.Id}] Loaded assembly: {Path.GetFileName(full)}");
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"  [{manifest.Id}] Failed to load {full}: {ex.Message}");
                    }
                }
            }

            return loaded.ToArray();
        }

        // ------------------------------------------------------------------ entrypoint invocation

        private void InvokeMainEntrypoints(BaldicLoaderImpl loader, List<ModContainer> mods)
        {
            foreach (var mod in mods)
            {
                if (mod.Manifest.Entrypoints?.Main == null) continue;

                foreach (string className in mod.Manifest.Entrypoints.Main)
                {
                    InvokeEntrypoint<IBaldicModInitializer>(
                        mod, className, "main",
                        (ep) =>
                        {
                            loader.RegisterEntrypoint("main", mod, ep);
                            ep.OnInitialize(new ModInitializationContext(mod, loader));
                            _log.Info($"  [{mod.Id}] OnInitialize OK");
                        });
                }
            }
        }

        private void InvokeEntrypoint<T>(
            ModContainer mod,
            string className,
            string key,
            Action<T> invoke) where T : class
        {
            Type? type = null;
            foreach (var asm in mod.Assemblies)
            {
                type = asm.GetType(className, throwOnError: false);
                if (type != null) break;
            }

            if (type == null)
            {
                _log.Warn($"  [{mod.Id}] Entrypoint class not found: {className}");
                return;
            }

            if (!typeof(T).IsAssignableFrom(type))
            {
                _log.Warn($"  [{mod.Id}] {className} does not implement {typeof(T).Name}");
                return;
            }

            try
            {
                T instance = (T)Activator.CreateInstance(type)!;
                invoke(instance);
            }
            catch (Exception ex)
            {
                _log.Error($"  [{mod.Id}] Entrypoint {className} threw: {ex}");
            }
        }
    }
}
