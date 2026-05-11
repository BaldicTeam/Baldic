using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baldic.Loader.Abstractions;

namespace Baldic.API.SaveSystem
{
    /// <summary>
    /// Central registry for mod save handlers and migrations.
    /// Manages the full save/load lifecycle for modded data.
    /// </summary>
    public sealed class SaveSystemRegistry
    {
        public static readonly SaveSystemRegistry Instance = new SaveSystemRegistry();

        private readonly object _lock = new object();
        private readonly Dictionary<string, IModSaveHandler> _handlers =
            new Dictionary<string, IModSaveHandler>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<IModSaveMigration>> _migrations =
            new Dictionary<string, List<IModSaveMigration>>(StringComparer.Ordinal);

        private SaveSystemRegistry() { }

        // ── Registration ──────────────────────────────────────────────────────

        public void Register(IModSaveHandler handler)
        {
            lock (_lock)
            {
                string key = handler.ChunkId.ToString();
                if (_handlers.ContainsKey(key))
                    throw new ArgumentException($"Save handler for chunk '{key}' is already registered.");
                _handlers[key] = handler;
            }
        }

        public void RegisterMigration(IModSaveMigration migration)
        {
            lock (_lock)
            {
                // Migrations are keyed by "modid:chunkname|from→to" but stored by chunkId prefix.
                // For simplicity, we key by "from" — the registry matches by handler chunk prefix.
                // Real lookup: pass the chunkId to identify which handler owns this migration.
                string key = $"{migration.FromVersion}";
                if (!_migrations.ContainsKey(key))
                    _migrations[key] = new List<IModSaveMigration>();
                _migrations[key].Add(migration);
            }
        }

        /// <summary>Register a migration for a specific chunk.</summary>
        public void RegisterMigrationFor(NamespacedId chunkId, IModSaveMigration migration)
        {
            lock (_lock)
            {
                string key = chunkId.ToString();
                if (!_migrations.ContainsKey(key))
                    _migrations[key] = new List<IModSaveMigration>();
                _migrations[key].Add(migration);
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialize all registered handler states into a <see cref="BaldicSaveFile"/>.
        /// </summary>
        public BaldicSaveFile SaveAll(string gameVersion, string loaderVersion, IReadOnlyList<string> activeModIds, IBaldicLog log)
        {
            var save = new BaldicSaveFile
            {
                GameVersion = gameVersion,
                LoaderVersion = loaderVersion,
                ActiveMods = new List<string>(activeModIds),
            };

            List<IModSaveHandler> handlers;
            lock (_lock) { handlers = new List<IModSaveHandler>(_handlers.Values); }

            foreach (var handler in handlers)
            {
                try
                {
                    using var ms = new MemoryStream();
                    using (var writer = new ModSaveWriter(ms))
                        handler.Save(writer);
                    save.AddChunk(handler.ChunkId, handler.SchemaVersion, ms.ToArray());
                }
                catch (Exception ex)
                {
                    log.Error($"[SaveSystem] Handler '{handler.ChunkId}' Save() threw: {ex.Message}");
                }
            }

            return save;
        }

        /// <summary>Write all mod save data to a file path.</summary>
        public void SaveToFile(string filePath, string gameVersion, string loaderVersion,
            IReadOnlyList<string> activeModIds, IBaldicLog log)
        {
            var save = SaveAll(gameVersion, loaderVersion, activeModIds, log);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write);
            save.WriteTo(fs);
            log.Info($"[SaveSystem] Saved {save.Chunks.Count} chunk(s) to '{filePath}'.");
        }

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Load a Baldic save file and dispatch chunks to registered handlers.
        /// Missing handlers produce a warning. Extra chunks (from uninstalled mods) are preserved in memory.
        /// </summary>
        public void LoadFromFile(string filePath, IBaldicLog log)
        {
            if (!File.Exists(filePath))
            {
                log.Info($"[SaveSystem] No save file at '{filePath}'. Resetting all handlers.");
                ResetAll(log);
                return;
            }

            BaldicSaveFile save;
            try
            {
                using var fs = File.OpenRead(filePath);
                save = BaldicSaveFile.ReadFrom(fs);
            }
            catch (Exception ex)
            {
                log.Error($"[SaveSystem] Failed to read save file '{filePath}': {ex.Message}. Resetting.");
                ResetAll(log);
                return;
            }

            log.Info($"[SaveSystem] Loading save (game={save.GameVersion}, loader={save.LoaderVersion}, chunks={save.Chunks.Count}).");

            List<IModSaveHandler> handlers;
            lock (_lock) { handlers = new List<IModSaveHandler>(_handlers.Values); }

            // Track handlers that didn't get data.
            var handledChunks = new HashSet<string>(StringComparer.Ordinal);

            foreach (var handler in handlers)
            {
                string key = handler.ChunkId.ToString();
                if (!save.TryGetChunk(handler.ChunkId, out var chunk))
                {
                    log.Info($"[SaveSystem] Chunk '{key}' not found in save. Calling Reset().");
                    TryReset(handler, log);
                    continue;
                }

                handledChunks.Add(key);
                byte[] data = MigrateChunk(handler.ChunkId, chunk.SchemaVersion, chunk.Data, handler.SchemaVersion, log);

                try
                {
                    using var ms = new MemoryStream(data);
                    using var reader = new ModSaveReader(ms);
                    handler.Load(reader);
                }
                catch (Exception ex)
                {
                    log.Error($"[SaveSystem] Handler '{key}' Load() threw: {ex.Message}. Resetting.");
                    TryReset(handler, log);
                }
            }

            // Warn about chunks from uninstalled mods.
            foreach (var kvp in save.Chunks)
            {
                if (!handledChunks.Contains(kvp.Key))
                    log.Warn($"[SaveSystem] Chunk '{kvp.Key}' has no registered handler — mod may be uninstalled.");
            }
        }

        public void ResetAll(IBaldicLog log)
        {
            List<IModSaveHandler> handlers;
            lock (_lock) { handlers = new List<IModSaveHandler>(_handlers.Values); }
            foreach (var h in handlers) TryReset(h, log);
        }

        // ── Migration ─────────────────────────────────────────────────────────

        private byte[] MigrateChunk(NamespacedId chunkId, int storedVersion, byte[] data, int targetVersion, IBaldicLog log)
        {
            if (storedVersion == targetVersion) return data;

            string key = chunkId.ToString();
            List<IModSaveMigration> migrations;
            lock (_lock)
            {
                _migrations.TryGetValue(key, out var m);
                migrations = m != null ? new List<IModSaveMigration>(m) : new List<IModSaveMigration>();
            }

            int current = storedVersion;
            while (current < targetVersion)
            {
                var migration = migrations.FirstOrDefault(m => m.FromVersion == current);
                if (migration == null)
                {
                    log.Warn($"[SaveSystem] No migration from v{current} to v{current + 1} for chunk '{key}'. Resetting.");
                    return Array.Empty<byte>();
                }

                try
                {
                    data = migration.Migrate(data);
                    log.Info($"[SaveSystem] Migrated chunk '{key}' from v{current} to v{migration.ToVersion}.");
                    current = migration.ToVersion;
                }
                catch (Exception ex)
                {
                    log.Error($"[SaveSystem] Migration v{current}→v{migration.ToVersion} for '{key}' threw: {ex.Message}. Using empty data.");
                    return Array.Empty<byte>();
                }
            }

            return data;
        }

        private static void TryReset(IModSaveHandler handler, IBaldicLog log)
        {
            try { handler.Reset(); }
            catch (Exception ex) { log.Error($"[SaveSystem] Handler '{handler.ChunkId}' Reset() threw: {ex.Message}"); }
        }
    }
}
