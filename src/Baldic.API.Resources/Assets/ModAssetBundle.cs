using System;
using System.IO;
using System.Reflection;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Resources.Assets
{
    /// <summary>
    /// Runtime implementation of <see cref="IModAssetBundle"/> that calls
    /// UnityEngine.AssetBundle via reflection, avoiding a compile-time
    /// dependency on Unity assemblies in this project.
    ///
    /// Unity AssetBundles must be built per-platform. The expected layout:
    /// <code>
    /// bundles/
    ///   windows/mybundle
    ///   linux/mybundle
    ///   macos/mybundle
    /// </code>
    /// </summary>
    public sealed class ModAssetBundle : IModAssetBundle
    {
        private object? _bundle; // UnityEngine.AssetBundle
        private bool _permanent;
        private bool _disposed;

        private static readonly Type? AssetBundleType =
            Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule") ??
            Type.GetType("UnityEngine.AssetBundle, UnityEngine");

        public string BundleName { get; }
        public bool IsLoaded => _bundle != null;

        private ModAssetBundle(string bundleName, object bundle)
        {
            BundleName = bundleName;
            _bundle = bundle;
        }

        /// <summary>
        /// Load a platform-appropriate AssetBundle from the mod's bundle directory.
        /// Selects linux/windows/macos subfolder automatically.
        /// </summary>
        public static ModAssetBundle? LoadFromDirectory(string bundleDir, string bundleName, IBaldicLog log)
        {
            if (AssetBundleType == null)
            {
                log.Error($"[AssetBundle] UnityEngine.AssetBundle type not found. Is the game running?");
                return null;
            }

            string platform = GetPlatformSubfolder();
            string path = Path.Combine(bundleDir, platform, bundleName);

            if (!File.Exists(path))
            {
                log.Warn($"[AssetBundle] Bundle not found at '{path}'. Trying root.");
                path = Path.Combine(bundleDir, bundleName);
                if (!File.Exists(path)) { log.Error($"[AssetBundle] Bundle '{bundleName}' not found."); return null; }
            }

            return LoadFromFile(path, log);
        }

        /// <summary>Load an AssetBundle from an explicit file path.</summary>
        public static ModAssetBundle? LoadFromFile(string path, IBaldicLog log)
        {
            if (AssetBundleType == null)
            {
                log.Error("[AssetBundle] UnityEngine.AssetBundle not available.");
                return null;
            }

            try
            {
                var loadMethod = AssetBundleType.GetMethod("LoadFromFile",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);

                if (loadMethod == null)
                {
                    log.Error("[AssetBundle] AssetBundle.LoadFromFile not found.");
                    return null;
                }

                object? bundle = loadMethod.Invoke(null, new object[] { path });
                if (bundle == null) { log.Error($"[AssetBundle] LoadFromFile returned null for '{path}'."); return null; }

                string name = Path.GetFileName(path);
                log.Info($"[AssetBundle] Loaded bundle '{name}' from {path}.");
                return new ModAssetBundle(name, bundle);
            }
            catch (Exception ex)
            {
                log.Error($"[AssetBundle] Failed to load '{path}': {ex.Message}");
                return null;
            }
        }

        public T? LoadAsset<T>(string name) where T : class
        {
            ThrowIfDisposed();
            if (_bundle == null) return null;

            try
            {
                var method = AssetBundleType!.GetMethod("LoadAsset",
                    new[] { typeof(string), typeof(Type) });
                var result = method?.Invoke(_bundle, new object[] { name, typeof(T) });
                return result as T;
            }
            catch { return null; }
        }

        public void MarkPermanent() => _permanent = true;

        public void Dispose()
        {
            if (_disposed || _permanent) return;
            _disposed = true;
            if (_bundle != null)
            {
                try
                {
                    var unload = AssetBundleType!.GetMethod("Unload", new[] { typeof(bool) });
                    unload?.Invoke(_bundle, new object[] { false });
                }
                catch { }
                _bundle = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ModAssetBundle));
        }

        private static string GetPlatformSubfolder()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT: return "windows";
                case PlatformID.Unix:    return "linux";
                case PlatformID.MacOSX:  return "macos";
                default:                 return "windows";
            }
        }
    }
}
