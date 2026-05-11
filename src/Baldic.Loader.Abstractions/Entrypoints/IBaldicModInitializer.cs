namespace Baldic.Loader.Abstractions.Entrypoints
{
    /// <summary>
    /// Main mod entrypoint. Implement this in the class referenced by <c>entrypoints.main</c>
    /// in <c>baldic.mod.json</c>. A public no-argument constructor is required.
    /// </summary>
    public interface IBaldicModInitializer
    {
        /// <summary>
        /// Called during <c>MainInitialize</c> lifecycle stage.
        /// Use this to set up registries, events and config bindings.
        /// Do not call Unity APIs here.
        /// </summary>
        void OnInitialize(ModInitializationContext context);
    }

    /// <summary>Context passed to <see cref="IBaldicModInitializer.OnInitialize"/>.</summary>
    public sealed class ModInitializationContext
    {
        public IModContainer Mod { get; }
        public IBaldicLoader Loader { get; }

        public ModInitializationContext(IModContainer mod, IBaldicLoader loader)
        {
            Mod = mod;
            Loader = loader;
        }
    }
}
