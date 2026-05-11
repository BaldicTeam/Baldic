namespace Baldic.Loader.Abstractions.Entrypoints
{
    /// <summary>
    /// Entrypoint for registering options menu categories.
    /// Implement this in the class referenced by <c>entrypoints.options</c>.
    /// </summary>
    public interface IBaldicOptionsEntrypoint
    {
        void RegisterOptions(OptionsRegistrationContext context);
    }

    public sealed class OptionsRegistrationContext
    {
        public IModContainer Mod { get; }
        public IBaldicLoader Loader { get; }

        public OptionsRegistrationContext(IModContainer mod, IBaldicLoader loader)
        {
            Mod = mod;
            Loader = loader;
        }
    }
}
