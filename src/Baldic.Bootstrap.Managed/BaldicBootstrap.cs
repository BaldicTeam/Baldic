using System;
using System.IO;
using System.Reflection;
using Baldic.Loader.Abstractions;
using Baldic.Loader.Logging;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Managed bootstrap entrypoint for Baldic.
    /// This class is invoked by Unity Doorstop before any game assemblies are loaded.
    ///
    /// Doorstop config (doorstop_config.ini / .doorstop_version):
    ///   target_assembly = Baldic/core/Baldic.Bootstrap.Managed.dll
    ///
    /// The static method <see cref="Initialize"/> must remain public and parameterless.
    /// </summary>
    public static class BaldicBootstrap
    {
        private static FileLogger? _log;
        private static bool _initialized;

        /// <summary>
        /// Called by Unity Doorstop. Must be public static void with no parameters.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            string gameRoot = BootstrapPaths.DetectGameRoot();
            string configPath = BootstrapPaths.ConfigFile(gameRoot);
            BootstrapConfig config = File.Exists(configPath)
                ? BootstrapConfig.Load(configPath)
                : BootstrapConfig.Default();

            // Resolve absolute log path.
            string logPath = Path.IsPathRooted(config.LogFile)
                ? config.LogFile
                : Path.Combine(gameRoot, config.LogFile);

            _log = new FileLogger("baldic-bootstrap", logPath);

            try
            {
                _log.Info($"Baldic Bootstrap {LoaderConstants.LoaderId} starting.");
                _log.Info($"Game root : {gameRoot}");
                _log.Info($"CLR       : {Environment.Version}");
                _log.Info($"OS        : {Environment.OSVersion}");

                if (!config.Enabled)
                {
                    _log.Warn("Baldic is DISABLED via baldic.cfg. Exiting bootstrap.");
                    return;
                }

                if (File.Exists(BootstrapPaths.SafeModeFlag(gameRoot)))
                {
                    _log.Warn("Safe-mode flag detected. Starting in safe mode — no mods will be loaded.");
                    return;
                }

                // Install AssemblyResolve ASAP so loader dependencies can be found.
                InstallAssemblyResolver(gameRoot, config);

                // Proceed to loader initialization.
                LoaderInit(gameRoot, config);
            }
            catch (Exception ex)
            {
                _log.Fatal($"Bootstrap crashed: {ex}");
                WriteCrashLog(gameRoot, ex);
            }
            // NOTE: _log is NOT disposed here — it must remain alive for scene-hook callbacks
            // (OnSceneLoaded fires after Initialize() returns). The OS will close the file on exit.
        }

        private static void InstallAssemblyResolver(string gameRoot, BootstrapConfig config)
        {
            string coreDir = Path.IsPathRooted(config.TargetAssembly)
                ? Path.GetDirectoryName(config.TargetAssembly)!
                : Path.Combine(gameRoot, Path.GetDirectoryName(config.TargetAssembly) ?? "Baldic/core");

            string libDir = Path.Combine(BootstrapPaths.BaldicRoot(gameRoot), "lib");
            string managedDir = BootstrapPaths.ManagedDir(gameRoot);

            _log!.Debug($"AssemblyResolve search paths:");
            _log.Debug($"  1. {coreDir}");
            _log.Debug($"  2. {libDir}");
            _log.Debug($"  3. {managedDir}");

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string name = new AssemblyName(args.Name).Name!;

                foreach (var dir in new[] { coreDir, libDir, managedDir })
                {
                    string path = Path.Combine(dir, name + ".dll");
                    if (File.Exists(path))
                    {
                        _log?.Debug($"Resolved '{name}' from {path}");
                        return Assembly.LoadFrom(path);
                    }
                }
                return null;
            };
        }

        private static void LoaderInit(string gameRoot, BootstrapConfig config)
        {
            // Loader initialization is deferred until this method to ensure
            // AssemblyResolve is active before Baldic.Loader types are referenced.
            var loaderRunner = new LoaderRunner(gameRoot, config, _log!);
            loaderRunner.Run();
        }

        private static void WriteCrashLog(string gameRoot, Exception ex)
        {
            try
            {
                string reportsDir = Path.Combine(BootstrapPaths.LogsDir(gameRoot),
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                Directory.CreateDirectory(reportsDir);
                File.WriteAllText(Path.Combine(reportsDir, "bootstrap-crash.txt"), ex.ToString());
            }
            catch { /* crash during crash report — ignore */ }
        }
    }
}

// Unity Doorstop v4 requires exactly this class + method signature.
// See: https://github.com/NeighTools/UnityDoorstop#v4-entrypoint
namespace Doorstop
{
    public class Entrypoint
    {
        public static void Start()
        {
            Baldic.Bootstrap.Managed.BaldicBootstrap.Initialize();
        }
    }
}
