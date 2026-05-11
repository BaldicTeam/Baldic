using System;
using System.IO;
using System.Text;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Core.Diagnostics
{
    /// <summary>
    /// Structured crash report writer.
    /// Writes to <c>Baldic/logs/reports/yyyy-MM-dd_HH-mm-ss/</c>.
    /// </summary>
    public static class ErrorReport
    {
        public static string Write(
            string reportsRoot,
            Exception ex,
            IBaldicLoader? loader,
            string? context = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string dir = Path.Combine(reportsRoot, timestamp);

            try
            {
                Directory.CreateDirectory(dir);

                // exception.txt
                File.WriteAllText(Path.Combine(dir, "exception.txt"), ex.ToString());

                // context.txt
                if (context != null)
                    File.WriteAllText(Path.Combine(dir, "context.txt"), context);

                // mod-list.json
                if (loader != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[");
                    var mods = loader.AllMods;
                    for (int i = 0; i < mods.Count; i++)
                    {
                        var m = mods[i];
                        sb.Append($"  {{ \"id\": \"{m.Id}\", \"version\": \"{m.Version}\" }}");
                        if (i < mods.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                    sb.AppendLine("]");
                    File.WriteAllText(Path.Combine(dir, "mod-list.json"), sb.ToString());
                }
            }
            catch { /* best-effort */ }

            return dir;
        }

        public static string FormatModError(
            string modId,
            string phase,
            Exception ex)
        {
            return $"[{phase}] Mod '{modId}' threw an exception:\n{ex}";
        }
    }
}
