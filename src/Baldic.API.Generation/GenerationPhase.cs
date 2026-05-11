namespace Baldic.API.Generation
{
    /// <summary>
    /// Ordered phases of the level generator modification pipeline.
    /// Port of MTM101BMDE <c>GenerationModType</c> with identical semantics.
    /// </summary>
    public enum GenerationPhase
    {
        /// <summary>
        /// Runs first. The ONLY phase where level type assignments (LevelObject array)
        /// on a SceneObject may be added or removed. Mutation outside this phase causes
        /// a hard error that identifies the offending mod.
        /// </summary>
        Preparation = 0,

        /// <summary>
        /// Large-scale generator transforms that effectively replace the default config.
        /// Use only when fundamentally changing a floor's generation.
        /// </summary>
        Base = 1,

        /// <summary>
        /// Targeted overrides: change specific generator properties (e.g. exit count).
        /// </summary>
        Override = 2,

        /// <summary>
        /// Additive content: add items, NPCs, posters to existing generation tables.
        /// Most mods should register here.
        /// </summary>
        Addend = 3,

        /// <summary>
        /// Final pass. Use sparingly — only for last-resort removals or fixes.
        /// </summary>
        Finalizer = 4,
    }
}
