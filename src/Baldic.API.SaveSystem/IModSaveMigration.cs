namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Migrates a save chunk from one schema version to the next.
    /// Implement one migration per version step (1→2, 2→3, etc.).
    /// The loader automatically chains migrations until the current <see cref="IModSaveHandler.SchemaVersion"/> is reached.
    /// </summary>
    public interface IModSaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }

        /// <summary>
        /// Transform the raw bytes from the old schema to the new schema.
        /// Return new byte[] with the migrated data.
        /// </summary>
        byte[] Migrate(byte[] oldData);
    }
}
