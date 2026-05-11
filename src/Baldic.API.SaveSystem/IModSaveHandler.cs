using System.Collections.Generic;
using Baldic.Loader.Abstractions;

namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Contract for a mod that wants to persist data across game sessions.
    /// Each handler owns one or more named chunks inside the Baldic save file.
    /// Vanilla save files are never touched.
    ///
    /// Usage:
    /// <code>
    /// public sealed class MySaveHandler : IModSaveHandler
    /// {
    ///     public NamespacedId ChunkId => new NamespacedId("my_mod", "main");
    ///     public int SchemaVersion => 1;
    ///     // ...
    /// }
    /// // Registration in IBaldicModInitializer.OnInitialize:
    /// context.SaveSystem.Register(new MySaveHandler());
    /// </code>
    /// </summary>
    public interface IModSaveHandler
    {
        /// <summary>
        /// Stable namespaced chunk identifier.
        /// Must be unique per mod. Store this in save files — never store raw ints.
        /// </summary>
        NamespacedId ChunkId { get; }

        /// <summary>
        /// Current schema version. Increment when the save format changes.
        /// The loader passes the stored version to <see cref="IModSaveMigration"/> if needed.
        /// </summary>
        int SchemaVersion { get; }

        /// <summary>Write the current mod state to the save writer.</summary>
        void Save(ModSaveWriter writer);

        /// <summary>
        /// Load mod state from the reader.
        /// Called only when the chunk is present and fully migrated to <see cref="SchemaVersion"/>.
        /// </summary>
        void Load(ModSaveReader reader);

        /// <summary>Reset mod state to defaults (called on new game or when chunk is missing).</summary>
        void Reset();

        /// <summary>
        /// Human-readable tags describing what this chunk stores.
        /// Shown in the missing-mod warning UI.
        /// Example: ["items", "player_data"]
        /// </summary>
        IEnumerable<string> GenerateTags();
    }
}
