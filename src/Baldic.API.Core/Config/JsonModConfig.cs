using System;
using System.IO;
using Newtonsoft.Json;

namespace Baldic.API.Core.Config
{
    /// <summary>
    /// JSON-backed config implementation. Stored at
    /// <c>Baldic/config/&lt;modid&gt;/&lt;key&gt;.json</c>.
    /// Thread-safe for single writer + multiple readers.
    /// </summary>
    public sealed class JsonModConfig<T> : IModConfig<T>
    {
        private readonly string _filePath;
        private readonly T _defaultValue;
        private readonly object _lock = new object();
        private T _value;

        public event Action<T>? Changed;

        public JsonModConfig(string filePath, T defaultValue)
        {
            _filePath = filePath;
            _defaultValue = defaultValue;
            _value = defaultValue;
        }

        public T Value
        {
            get { lock (_lock) { return _value; } }
            set { lock (_lock) { _value = value; } }
        }

        public void Save()
        {
            T snapshot;
            lock (_lock) { snapshot = _value; }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Reload()
        {
            if (!File.Exists(_filePath)) return;
            try
            {
                string json = File.ReadAllText(_filePath);
                var loaded = JsonConvert.DeserializeObject<T>(json);
                T newVal = loaded ?? _defaultValue;
                lock (_lock) { _value = newVal; }
                Changed?.Invoke(newVal);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Baldic.Config] Failed to reload '{_filePath}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Factory for creating per-mod config instances.
    /// </summary>
    public sealed class ModConfigFactory
    {
        private readonly string _configRoot;
        private readonly string _modId;

        public ModConfigFactory(string configRoot, string modId)
        {
            _configRoot = configRoot;
            _modId = modId;
        }

        /// <summary>
        /// Create or load a typed config stored at
        /// <c>Baldic/config/&lt;modid&gt;/&lt;key&gt;.json</c>.
        /// </summary>
        public IModConfig<T> Bind<T>(string key, T defaultValue)
        {
            string path = Path.Combine(_configRoot, _modId, $"{key}.json");
            var config = new JsonModConfig<T>(path, defaultValue);
            config.Reload();
            return config;
        }
    }
}
