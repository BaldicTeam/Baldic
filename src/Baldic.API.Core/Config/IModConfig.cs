using System;

namespace Baldic.API.Core.Config
{
    /// <summary>
    /// Typed config binding for a mod. Config is persisted to
    /// <c>Baldic/config/&lt;modid&gt;.json</c> and reloaded on each game launch.
    /// </summary>
    public interface IModConfig<T>
    {
        /// <summary>Current value. Returns default if not yet loaded.</summary>
        T Value { get; set; }

        /// <summary>Persist the current value to disk.</summary>
        void Save();

        /// <summary>Reload the value from disk (overwrites unsaved changes).</summary>
        void Reload();

        /// <summary>Fired after <see cref="Reload"/> completes with a new value.</summary>
        event Action<T> Changed;
    }
}
