namespace Baldic.Loader.Resolver
{
    public enum ModProblemKind
    {
        /// <summary>Required dependency not found or version mismatch.</summary>
        MissingDependency,
        /// <summary>A declared 'breaks' entry is present.</summary>
        BreaksViolation,
        /// <summary>Two mods have the same id.</summary>
        DuplicateId,
        /// <summary>Circular dependency detected.</summary>
        CircularDependency,
        /// <summary>Incompatible game version range.</summary>
        GameVersionMismatch,
        /// <summary>Incompatible loader version range.</summary>
        LoaderVersionMismatch,
        /// <summary>Manifest could not be parsed.</summary>
        InvalidManifest
    }

    /// <summary>A problem found during dependency resolution.</summary>
    public sealed class ModProblem
    {
        public ModProblemKind Kind { get; }
        public string ModId { get; }
        public string Message { get; }

        public ModProblem(ModProblemKind kind, string modId, string message)
        {
            Kind = kind;
            ModId = modId;
            Message = message;
        }

        public override string ToString() => $"[{Kind}] {ModId}: {Message}";
    }
}
