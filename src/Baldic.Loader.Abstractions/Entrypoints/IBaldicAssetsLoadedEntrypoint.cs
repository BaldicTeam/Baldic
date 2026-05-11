using System.Collections;

namespace Baldic.Loader.Abstractions.Entrypoints
{
    /// <summary>
    /// Coroutine entrypoint called during the assets loading phase.
    /// Implement this in the class referenced by <c>entrypoints.assetsLoaded</c>.
    /// </summary>
    public interface IBaldicAssetsLoadedEntrypoint
    {
        /// <summary>
        /// Unity coroutine. Yield <c>null</c> to wait one frame.
        /// Use <see cref="IProgressReporter"/> to update loading screen.
        /// </summary>
        IEnumerator OnAssetsLoaded(AssetsLoadedContext context);
    }

    public sealed class AssetsLoadedContext
    {
        public IModContainer Mod { get; }
        public IBaldicLoader Loader { get; }
        public IProgressReporter Progress { get; }

        public AssetsLoadedContext(IModContainer mod, IBaldicLoader loader, IProgressReporter progress)
        {
            Mod = mod;
            Loader = loader;
            Progress = progress;
        }
    }

    public interface IProgressReporter
    {
        void SetTotal(int total);
        void Step(string message);
        void Warning(string message);
    }
}
