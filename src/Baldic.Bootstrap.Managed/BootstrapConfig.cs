using System;
using System.Collections.Generic;
using System.IO;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// Reads <c>baldic.cfg</c> INI-style config file.
    /// </summary>
    internal sealed class BootstrapConfig
    {
        public bool Enabled { get; private set; } = true;
        public string TargetAssembly { get; private set; } = "Baldic/core/Baldic.Bootstrap.Managed.dll";
        public string LogFile { get; private set; } = "Baldic/logs/bootstrap.log";
        public string ModsDir { get; private set; } = "Baldic/mods";
        public string CacheDir { get; private set; } = "Baldic/cache";
        public string ConfigDir { get; private set; } = "Baldic/config";

        public static BootstrapConfig Load(string configPath)
        {
            var cfg = new BootstrapConfig();
            if (!File.Exists(configPath)) return cfg;

            string? section = null;
            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).ToLowerInvariant();
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = line.Substring(eq + 1).Trim();

                switch (section)
                {
                    case "bootstrap":
                        if (key == "enabled") cfg.Enabled = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
                        if (key == "log_file") cfg.LogFile = value;
                        break;
                    case "loader":
                        if (key == "mods_dir") cfg.ModsDir = value;
                        if (key == "cache_dir") cfg.CacheDir = value;
                        if (key == "config_dir") cfg.ConfigDir = value;
                        break;
                }
            }

            return cfg;
        }

        public static BootstrapConfig Default() => new BootstrapConfig();
    }
}
