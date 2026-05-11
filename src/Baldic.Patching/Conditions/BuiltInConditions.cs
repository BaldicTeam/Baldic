using System;
using System.IO;
using Baldic.Loader.Abstractions;

namespace Baldic.Patching.Conditions
{
    /// <summary>Patch is always applied. Equivalent to having no condition.</summary>
    public sealed class AlwaysPatchCondition : IBaldicPatchCondition
    {
        public bool ShouldPatch(PatchConditionContext context) => true;
    }

    /// <summary>Patch is never applied. Useful to quickly disable a patch during development.</summary>
    public sealed class NeverPatchCondition : IBaldicPatchCondition
    {
        public bool ShouldPatch(PatchConditionContext context) => false;
    }

    /// <summary>
    /// Apply the patch only if the specified mod is loaded.
    /// Port of MTM101BMDE <c>ConditionalPatchMod</c>.
    /// </summary>
    public sealed class ModLoadedCondition : IBaldicPatchCondition
    {
        private readonly string _modId;

        public ModLoadedCondition(string modId)
        {
            _modId = modId;
        }

        public bool ShouldPatch(PatchConditionContext context) =>
            context.Loader.IsModLoaded(_modId);
    }

    /// <summary>
    /// Apply the patch only if the specified mod is NOT loaded.
    /// Port of MTM101BMDE <c>ConditionalPatchNoMod</c>.
    /// </summary>
    public sealed class ModMissingCondition : IBaldicPatchCondition
    {
        private readonly string _modId;

        public ModMissingCondition(string modId)
        {
            _modId = modId;
        }

        public bool ShouldPatch(PatchConditionContext context) =>
            !context.Loader.IsModLoaded(_modId);
    }

    /// <summary>
    /// Apply the patch only if the current game version matches the specified range.
    /// Example: <c>new GameVersionCondition(">=0.14.0 &lt;0.15.0")</c>
    /// </summary>
    public sealed class GameVersionCondition : IBaldicPatchCondition
    {
        private readonly string _rangeExpression;

        public GameVersionCondition(string rangeExpression)
        {
            _rangeExpression = rangeExpression;
        }

        public bool ShouldPatch(PatchConditionContext context)
        {
            var gameMod = context.Loader.GetMod(LoaderConstants.GameId);
            if (gameMod == null) return false;

            if (!VersionRange.TryParse(_rangeExpression, out var range, out _))
                return false;

            return range!.Matches(gameMod.Version);
        }
    }

    /// <summary>
    /// Apply the patch only if a named config value (in the requesting mod's config) is true.
    /// The config file is expected at <c>Baldic/config/&lt;modid&gt;.json</c>.
    /// Key format: "Section.Name" or just "Name".
    /// </summary>
    public sealed class ConfigBoolCondition : IBaldicPatchCondition
    {
        private readonly string _key;

        public ConfigBoolCondition(string key)
        {
            _key = key;
        }

        public bool ShouldPatch(PatchConditionContext context)
        {
            string path = Path.Combine(
                context.ConfigRoot, context.RequestingMod.Id, _key + ".json");

            if (!File.Exists(path)) return true;

            try
            {
                string content = File.ReadAllText(path).Trim().Trim('"');
                if (bool.TryParse(content, out bool val)) return val;
                return !string.Equals(content, "false", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }
    }
}
