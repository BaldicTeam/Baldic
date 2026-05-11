namespace Baldic.Loader.Abstractions.Entrypoints
{
    /// <summary>
    /// Entrypoint for registering level generator modifications.
    /// Implement this in the class referenced by <c>entrypoints.generator</c>.
    /// </summary>
    public interface IBaldicGeneratorEntrypoint
    {
        void RegisterGeneratorChanges(GeneratorRegistrationContext context);
    }

    public sealed class GeneratorRegistrationContext
    {
        public IModContainer Mod { get; }
        public IBaldicLoader Loader { get; }

        public GeneratorRegistrationContext(IModContainer mod, IBaldicLoader loader)
        {
            Mod = mod;
            Loader = loader;
        }
    }
}
