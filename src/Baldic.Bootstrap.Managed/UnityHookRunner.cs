using System;
using System.Collections.Generic;
using Baldic.API.UI.LoadingScreen;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Abstractions.Entrypoints;
using UnityEngine.SceneManagement;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Invokes entrypoints at the correct lifecycle phase.
    ///
    /// <c>preLaunch</c> + <c>generator</c> — invoked immediately (no Unity needed).
    /// <c>assetsLoaded</c> + <c>options</c> — invoked on first <c>SceneManager.sceneLoaded</c>.
    /// </summary>
    internal static class UnityHookRunner
    {
        private static volatile bool _sceneFired;
        private static BaldicLoaderImpl? _loader;
        private static List<ModContainer>? _mods;
        private static IBaldicLog? _log;

        public static void Apply(
            IBaldicLog log,
            BaldicLoaderImpl loader,
            List<ModContainer> mods,
            object _ = null!)
        {
            _log    = log;
            _loader = loader;
            _mods   = mods;

            InvokePreLaunch();
            InvokeGenerator();
            InstallSceneHook(log);
        }

        // ------------------------------------------------------------------ scene hook

        private static void InstallSceneHook(IBaldicLog log)
        {
            try
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                log.Info("[UnityHook] sceneLoaded hook installed.");
            }
            catch (Exception ex)
            {
                log.Warn($"[UnityHook] sceneLoaded hook failed: {ex.Message}");
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_sceneFired) return;
            _sceneFired = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            try
            {
                _log?.Info($"[UnityHook] Scene '{scene.name}' loaded — invoking assetsLoaded + options.");
                InvokeAssetsLoaded();
                InvokeOptions();
                SpawnModListHost();
                _log?.Info("[UnityHook] Post-scene entrypoints complete.");
            }
            catch (Exception ex)
            {
                _log?.Error($"[UnityHook] Post-scene error: {ex}");
            }
        }

        private static void SpawnModListHost()
        {
            if (_mods == null || _loader == null) return;
            try
            {
                var go = new UnityEngine.GameObject("Baldic.ModListHost");
                var behaviour = go.AddComponent<ModListBehaviour>();
                behaviour.Init(_loader.AllMods);
                _log?.Info("[ModList] Mod list host created (F1 to toggle).");
            }
            catch (Exception ex)
            {
                _log?.Warn($"[ModList] Failed to create mod list host: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ entrypoint invocation

        private static void InvokePreLaunch()
        {
            if (_mods == null || _loader == null) return;
            foreach (var mod in _mods)
            {
                if (mod.Manifest.Entrypoints?.PreLaunch == null) continue;
                foreach (string cls in mod.Manifest.Entrypoints.PreLaunch)
                {
                    InvokeEntrypointClass<IBaldicPreLaunchEntrypoint>(mod, cls, ep =>
                    {
                        _loader.RegisterEntrypoint("preLaunch", mod, ep);
                        ep.OnPreLaunch(new PreLaunchContext(mod, _loader));
                        _log?.Info($"  [{mod.Id}] OnPreLaunch OK");
                    });
                }
            }
        }

        private static void InvokeGenerator()
        {
            if (_mods == null || _loader == null) return;
            foreach (var mod in _mods)
            {
                if (mod.Manifest.Entrypoints?.Generator == null) continue;
                foreach (string cls in mod.Manifest.Entrypoints.Generator)
                {
                    InvokeEntrypointClass<IBaldicGeneratorEntrypoint>(mod, cls, ep =>
                    {
                        _loader.RegisterEntrypoint("generator", mod, ep);
                        ep.RegisterGeneratorChanges(new GeneratorRegistrationContext(mod, _loader));
                        _log?.Info($"  [{mod.Id}] RegisterGeneratorChanges OK");
                    });
                }
            }
        }

        private static void InvokeEntrypointClass<T>(
            ModContainer mod, string className, Action<T> invoke) where T : class
        {
            Type? type = null;
            foreach (var asm in mod.Assemblies)
            {
                type = asm.GetType(className, throwOnError: false);
                if (type != null) break;
            }

            if (type == null)
            {
                _log?.Warn($"  [{mod.Id}] Entrypoint not found: {className}");
                return;
            }

            if (!typeof(T).IsAssignableFrom(type))
            {
                _log?.Warn($"  [{mod.Id}] {className} does not implement {typeof(T).Name}");
                return;
            }

            try { invoke((T)Activator.CreateInstance(type)!); }
            catch (Exception ex) { _log?.Error($"  [{mod.Id}] {className} threw: {ex}"); }
        }

        private static void InvokeAssetsLoaded()
        {
            if (_mods == null || _loader == null) return;
            var progress = NullProgressReporter.Instance;
            foreach (var mod in _mods)
            {
                if (mod.Manifest.Entrypoints?.AssetsLoaded == null) continue;
                foreach (string cls in mod.Manifest.Entrypoints.AssetsLoaded)
                {
                    InvokeEntrypointClass<IBaldicAssetsLoadedEntrypoint>(mod, cls, ep =>
                    {
                        _loader.RegisterEntrypoint("assetsLoaded", mod, ep);
                        ep.OnAssetsLoaded(new AssetsLoadedContext(mod, _loader, progress));
                        _log?.Info($"  [{mod.Id}] OnAssetsLoaded OK");
                    });
                }
            }
        }

        private static void InvokeOptions()
        {
            if (_mods == null || _loader == null) return;
            foreach (var mod in _mods)
            {
                if (mod.Manifest.Entrypoints?.Options == null) continue;
                foreach (string cls in mod.Manifest.Entrypoints.Options)
                {
                    InvokeEntrypointClass<IBaldicOptionsEntrypoint>(mod, cls, ep =>
                    {
                        _loader.RegisterEntrypoint("options", mod, ep);
                        ep.RegisterOptions(new OptionsRegistrationContext(mod, _loader));
                        _log?.Info($"  [{mod.Id}] RegisterOptions OK");
                    });
                }
            }
        }
    }
}
