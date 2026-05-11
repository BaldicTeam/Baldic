namespace Baldic.API.Core.Lifecycle
{
    /// <summary>
    /// Ordered lifecycle stages for Baldic.
    /// Mods should only use APIs appropriate to the current stage.
    /// </summary>
    public enum LifecycleStage
    {
        /// <summary>Initial state — loader has not started.</summary>
        None = 0,

        /// <summary>Bootstrap started; only loader internals, no Unity APIs.</summary>
        BootstrapStart = 1,

        /// <summary>Pre-launch: before Unity engine finishes initialization. Use with extreme caution.</summary>
        PreLaunch = 2,

        /// <summary>Manifests resolved, assemblies loadable. No Unity API yet.</summary>
        LoaderInitialized = 3,

        /// <summary>Registry/event setup phase. Harmony patches may be applied here.</summary>
        MainInitialize = 4,

        /// <summary>Game assemblies loaded and available. Harmony patches complete.</summary>
        GameAssembliesReady = 5,

        /// <summary>Resources.FindObjectsOfTypeAll is available.</summary>
        GameResourcesDiscovered = 6,

        /// <summary>Assets loading phase started (coroutine entrypoints run here).</summary>
        AssetsLoadedStart = 7,

        /// <summary>After asset loading, before generator modification.</summary>
        AssetsLoadedPreGeneration = 8,

        /// <summary>Generator modification phases run.</summary>
        GeneratorModification = 9,

        /// <summary>After generator modification.</summary>
        AssetsLoadedPostGeneration = 10,

        /// <summary>Localization tables are fully merged.</summary>
        LocalizationReady = 11,

        /// <summary>All initialization complete. Game is ready to play.</summary>
        Ready = 12,

        /// <summary>Game is shutting down.</summary>
        Shutdown = 99,
    }
}
