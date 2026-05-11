using Baldic.Loader.Abstractions;

namespace Baldic.Patching.Conditions
{
    /// <summary>
    /// A condition that determines whether a Harmony patch class should be applied.
    /// Implement this interface and pass the type to <see cref="Attributes.BaldicPatchConditionAttribute"/>.
    ///
    /// Implementations must have a public no-argument constructor.
    /// </summary>
    public interface IBaldicPatchCondition
    {
        /// <summary>
        /// Called once during patch registration phase.
        /// Return <c>true</c> to apply the patch; <c>false</c> to skip it.
        /// </summary>
        bool ShouldPatch(PatchConditionContext context);
    }

    /// <summary>
    /// Context passed to <see cref="IBaldicPatchCondition.ShouldPatch"/>.
    /// </summary>
    public sealed class PatchConditionContext
    {
        /// <summary>The Baldic loader facade. Available after <c>LoaderInitialized</c>.</summary>
        public IBaldicLoader Loader { get; }

        /// <summary>The mod that owns this patch.</summary>
        public IModContainer RequestingMod { get; }

        /// <summary>
        /// Root directory for mod configs: <c>Baldic/config/</c>.
        /// A mod's config files live at <c>ConfigRoot/&lt;modId&gt;/&lt;key&gt;.json</c>.
        /// </summary>
        public string ConfigRoot { get; }

        public PatchConditionContext(IBaldicLoader loader, IModContainer requestingMod, string configRoot)
        {
            Loader = loader;
            RequestingMod = requestingMod;
            ConfigRoot = configRoot;
        }
    }
}
