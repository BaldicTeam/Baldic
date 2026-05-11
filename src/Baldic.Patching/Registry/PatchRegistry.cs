using System.Collections.Generic;
using System.Linq;

namespace Baldic.Patching.Registry
{
    /// <summary>
    /// Global registry of all Harmony patches applied (or skipped) by Baldic mods.
    /// Thread-safe for reads after patching phase is complete.
    /// </summary>
    public sealed class PatchRegistry
    {
        private readonly object _lock = new object();
        private readonly List<PatchRecord> _records = new List<PatchRecord>();

        public static readonly PatchRegistry Instance = new PatchRegistry();

        private PatchRegistry() { }

        public void Record(PatchRecord record)
        {
            lock (_lock)
            {
                _records.Add(record);
            }
        }

        public IReadOnlyList<PatchRecord> GetAll()
        {
            lock (_lock) { return _records.ToList(); }
        }

        public IReadOnlyList<PatchRecord> GetByMod(string modId)
        {
            lock (_lock)
            {
                return _records.Where(r => r.OwnerModId == modId).ToList();
            }
        }

        public IReadOnlyList<PatchRecord> GetByTarget(string targetMethodFullName)
        {
            lock (_lock)
            {
                return _records.Where(r => r.TargetMethodFullName == targetMethodFullName).ToList();
            }
        }

        public IReadOnlyList<PatchRecord> GetFailed()
        {
            lock (_lock)
            {
                return _records.Where(r => r.Error != null).ToList();
            }
        }

        /// <summary>
        /// Write a human-readable summary of all patches to a string.
        /// Useful for the patches.log diagnostic file.
        /// </summary>
        public string FormatReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Baldic Patch Registry ===");
            IReadOnlyList<PatchRecord> all;
            lock (_lock) { all = _records.ToList(); }

            foreach (var g in all.GroupBy(r => r.OwnerModId).OrderBy(g => g.Key))
            {
                sb.AppendLine($"  [{g.Key}]");
                foreach (var r in g)
                    sb.AppendLine($"    {r}");
            }
            sb.AppendLine($"Total: {all.Count} patches, {all.Count(r => !r.Applied)} skipped, {all.Count(r => r.Error != null)} errors.");
            return sb.ToString();
        }
    }
}
