using System;
using System.Collections.Generic;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Core.Lifecycle
{
    /// <summary>
    /// Central lifecycle event bus.
    /// Mods subscribe to stage transitions via <see cref="OnStageEnter"/>.
    /// The loader advances stages by calling <see cref="Advance"/>.
    /// </summary>
    public sealed class LifecycleBus
    {
        public static readonly LifecycleBus Instance = new LifecycleBus();

        private LifecycleStage _current = LifecycleStage.None;
        private readonly IBaldicLog? _log;
        private readonly object _lock = new object();

        private readonly Dictionary<LifecycleStage, List<Action<LifecycleStage>>> _handlers =
            new Dictionary<LifecycleStage, List<Action<LifecycleStage>>>();

        private LifecycleBus() { }

        public LifecycleStage Current
        {
            get { lock (_lock) { return _current; } }
        }

        /// <summary>
        /// Subscribe to a specific lifecycle stage.
        /// If the stage has already passed, the handler is invoked immediately.
        /// </summary>
        public void OnStageEnter(LifecycleStage stage, Action<LifecycleStage> handler)
        {
            lock (_lock)
            {
                if (_current >= stage)
                {
                    // Already past this stage — call immediately.
                    SafeInvoke(handler, stage);
                    return;
                }

                if (!_handlers.TryGetValue(stage, out var list))
                {
                    list = new List<Action<LifecycleStage>>();
                    _handlers[stage] = list;
                }
                list.Add(handler);
            }
        }

        /// <summary>
        /// Advance to the next stage and fire all registered handlers.
        /// Stages must be advanced in order; skipping stages is not allowed.
        /// </summary>
        public void Advance(LifecycleStage newStage)
        {
            List<Action<LifecycleStage>>? handlers;
            lock (_lock)
            {
                if (newStage <= _current)
                    throw new InvalidOperationException(
                        $"Cannot advance lifecycle from {_current} to {newStage} (must be forward).");
                _current = newStage;
                _handlers.TryGetValue(newStage, out handlers);
            }

            if (handlers != null)
            {
                foreach (var h in handlers)
                    SafeInvoke(h, newStage);
            }
        }

        private void SafeInvoke(Action<LifecycleStage> handler, LifecycleStage stage)
        {
            try
            {
                handler(stage);
            }
            catch (Exception ex)
            {
                // Log but do not rethrow — one bad handler must not break the rest.
                Console.Error.WriteLine($"[LifecycleBus] Handler for {stage} threw: {ex}");
            }
        }
    }
}
