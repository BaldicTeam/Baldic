namespace Baldic.Loader.Scanning
{
    public enum ModCandidateKind
    {
        /// <summary>A packed .baldicmod zip archive.</summary>
        PackedArchive,
        /// <summary>An exploded directory containing baldic.mod.json.</summary>
        ExplodedDirectory,
        /// <summary>A .baldicdev link file pointing to a build output.</summary>
        DevLink
    }
}
