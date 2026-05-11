using System;
using System.Collections.Generic;
using System.IO;
using Baldic.Loader.Abstractions;
using Newtonsoft.Json;

namespace Baldic.API.Resources.Localization
{
    /// <summary>
    /// Registry for mod localization data.
    /// Mods add file paths or in-memory providers; the registry merges them
    /// into the game's <c>LocalizationManager</c> at <c>LocalizationReady</c>.
    ///
    /// Key collision policy:
    ///   - Same owner may override its own keys silently.
    ///   - Different owner override logs a warning unless declared in manifest.
    /// </summary>
    public sealed class LocalizationRegistry
    {
        public static readonly LocalizationRegistry Instance = new LocalizationRegistry();

        private readonly object _lock = new object();
        private readonly List<LocalizationEntry> _entries = new List<LocalizationEntry>();

        private LocalizationRegistry() { }

        /// <summary>
        /// Queue a JSON localization file. Keys must be namespaced: "modid.category.key".
        /// File format: <c>{ "key": "value", ... }</c>
        /// </summary>
        public void AddFile(string language, string absolutePath, IModContainer owner)
        {
            lock (_lock)
                _entries.Add(new LocalizationEntry(language, absolutePath, null, owner));
        }

        /// <summary>
        /// Queue an in-memory provider function called at merge time.
        /// </summary>
        public void AddProvider(string language, Func<Dictionary<string, string>> provider, IModContainer owner)
        {
            lock (_lock)
                _entries.Add(new LocalizationEntry(language, null, provider, owner));
        }

        /// <summary>
        /// Merge all registered entries for the given language into the supplied dictionary.
        /// Called by Baldic.API.Core at <c>LocalizationReady</c>.
        /// </summary>
        public void MergeInto(string language, Dictionary<string, string> target, IBaldicLog log)
        {
            List<LocalizationEntry> entries;
            lock (_lock)
                entries = new List<LocalizationEntry>(_entries);

            // Track per-key owners for collision warnings.
            var keyOwners = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var entry in entries)
            {
                if (!string.Equals(entry.Language, language, StringComparison.OrdinalIgnoreCase))
                    continue;

                Dictionary<string, string>? data = LoadEntry(entry, log);
                if (data == null) continue;

                foreach (var kvp in data)
                {
                    if (keyOwners.TryGetValue(kvp.Key, out string? existingOwner))
                    {
                        if (existingOwner != entry.Owner.Id)
                            log.Warn($"[Localization] Key '{kvp.Key}' overridden by '{entry.Owner.Id}' (was owned by '{existingOwner}').");
                    }
                    target[kvp.Key] = kvp.Value;
                    keyOwners[kvp.Key] = entry.Owner.Id;
                }
            }
        }

        private static Dictionary<string, string>? LoadEntry(LocalizationEntry entry, IBaldicLog log)
        {
            try
            {
                if (entry.Provider != null)
                    return entry.Provider();

                if (entry.FilePath != null && File.Exists(entry.FilePath))
                {
                    string json = File.ReadAllText(entry.FilePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                        ?? new Dictionary<string, string>();
                }

                log.Warn($"[Localization] File not found: {entry.FilePath}");
                return null;
            }
            catch (Exception ex)
            {
                log.Error($"[Localization] Failed to load entry for '{entry.Owner.Id}': {ex.Message}");
                return null;
            }
        }

        private sealed class LocalizationEntry
        {
            public string Language { get; }
            public string? FilePath { get; }
            public Func<Dictionary<string, string>>? Provider { get; }
            public IModContainer Owner { get; }

            public LocalizationEntry(string language, string? filePath,
                Func<Dictionary<string, string>>? provider, IModContainer owner)
            {
                Language = language;
                FilePath = filePath;
                Provider = provider;
                Owner = owner;
            }
        }
    }
}
