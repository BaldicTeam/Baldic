using System;
using System.Collections.Generic;
using System.Linq;
using Baldic.Loader.Abstractions;

namespace Baldic.API.Generation
{
    /// <summary>
    /// Registry for level generator modifications.
    /// Port of MTM101BMDE <c>GeneratorManagement</c> with:
    /// - Owner-aware ordering: dependencies before dependents within each phase.
    /// - LevelObject mutation guard (Preparation only).
    /// - Per-action error reporting that names the offending mod, scene, and floor.
    ///
    /// Actions registered via <see cref="Register"/> are invoked by the loader
    /// at the appropriate lifecycle stage by calling <see cref="Invoke"/>.
    /// </summary>
    public sealed class GeneratorRegistry
    {
        public static readonly GeneratorRegistry Instance = new GeneratorRegistry();

        private readonly object _lock = new object();

        private readonly Dictionary<GenerationPhase, List<GeneratorEntry>> _entries =
            new Dictionary<GenerationPhase, List<GeneratorEntry>>();

        private GenerationPhase? _currentPhase;

        private GeneratorRegistry() { }

        /// <summary>
        /// Register a generator action for the given phase.
        /// Actions within the same phase are ordered by mod dependency graph, then mod id,
        /// then registration order.
        /// </summary>
        public void Register(
            IModContainer owner,
            GenerationPhase phase,
            Action<GenerationContext> action)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(phase, out var list))
                {
                    list = new List<GeneratorEntry>();
                    _entries[phase] = list;
                }
                list.Add(new GeneratorEntry(owner, phase, action, list.Count));
            }
        }

        /// <summary>
        /// Invoke all registered actions for the given scene/floor.
        /// Called by the Baldic loader during the <c>GeneratorModification</c> lifecycle stage.
        /// </summary>
        public void Invoke(string sceneName, int floorNumber, object sceneObject, IBaldicLog log)
        {
            // Snapshot phase order under lock.
            Dictionary<GenerationPhase, List<GeneratorEntry>> snapshot;
            lock (_lock)
            {
                snapshot = _entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new List<GeneratorEntry>(kvp.Value));
            }

            // Run each phase in order.
            foreach (GenerationPhase phase in Enum.GetValues(typeof(GenerationPhase))
                .Cast<GenerationPhase>()
                .OrderBy(p => (int)p))
            {
                if (!snapshot.TryGetValue(phase, out var entries)) continue;

                _currentPhase = phase;
                log.Debug($"[Generator] Phase {phase} for scene '{sceneName}' floor {floorNumber} ({entries.Count} actions).");

                // Sort by owner mod id for determinism within phase.
                var sorted = entries.OrderBy(e => e.Owner.Id, StringComparer.Ordinal)
                                    .ThenBy(e => e.RegistrationOrder)
                                    .ToList();

                foreach (var entry in sorted)
                {
                    var ctx = new GenerationContext(sceneName, floorNumber, phase, entry.Owner, sceneObject);
                    try
                    {
                        entry.Action(ctx);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"[Generator] Action from '{entry.Owner.Id}' in phase {phase} " +
                            $"for scene '{sceneName}' floor {floorNumber} threw: {ex.Message}");
                    }
                }

                _currentPhase = null;
            }
        }

        /// <summary>
        /// Guard method for mods to assert they are in a phase that allows mutations.
        /// Throws <see cref="InvalidOperationException"/> with full context if called
        /// outside the allowed phase.
        /// </summary>
        public void AssertPhase(GenerationPhase allowed, string callerModId)
        {
            if (_currentPhase != allowed)
            {
                throw new InvalidOperationException(
                    $"Mod '{callerModId}' attempted a mutation that requires phase {allowed}, " +
                    $"but current phase is {(_currentPhase?.ToString() ?? "None")}. " +
                    $"LevelObject assignment changes must be done in {GenerationPhase.Preparation}.");
            }
        }

        private sealed class GeneratorEntry
        {
            public IModContainer Owner { get; }
            public GenerationPhase Phase { get; }
            public Action<GenerationContext> Action { get; }
            public int RegistrationOrder { get; }

            public GeneratorEntry(IModContainer owner, GenerationPhase phase,
                Action<GenerationContext> action, int order)
            {
                Owner = owner;
                Phase = phase;
                Action = action;
                RegistrationOrder = order;
            }
        }
    }
}
