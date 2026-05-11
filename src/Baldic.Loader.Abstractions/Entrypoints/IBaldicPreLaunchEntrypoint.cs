namespace Baldic.Loader.Abstractions.Entrypoints
{
    /// <summary>
    /// Called before Unity engine finishes initialization. Use with extreme caution —
    /// Unity APIs are not safe to call here. Intended for early assembly patching only.
    /// </summary>
    public interface IBaldicPreLaunchEntrypoint
    {
        void OnPreLaunch(PreLaunchContext context);
    }

    public sealed class PreLaunchContext
    {
        public IModContainer Mod { get; }
        public IBaldicLoader Loader { get; }

        public PreLaunchContext(IModContainer mod, IBaldicLoader loader)
        {
            Mod = mod;
            Loader = loader;
        }
    }
}
