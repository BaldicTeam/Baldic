using Baldic.Loader.Abstractions;

namespace Baldic.API.Generation
{
    /// <summary>
    /// Context passed to a generator action.
    /// Wraps scene/floor identity and the current generation phase so mods can
    /// assert they are not in the wrong phase.
    /// </summary>
    public sealed class GenerationContext
    {
        /// <summary>Scene name, e.g. "F1" or "F2".</summary>
        public string SceneName { get; }

        /// <summary>Floor number (0-indexed).</summary>
        public int FloorNumber { get; }

        /// <summary>Current phase; throws <see cref="System.InvalidOperationException"/> if mutated outside Preparation.</summary>
        public GenerationPhase CurrentPhase { get; }

        /// <summary>The mod that registered this action.</summary>
        public IModContainer Owner { get; }

        /// <summary>
        /// The SceneObject being modified.
        /// Cast to UnityEngine SceneObject type at the call site.
        /// </summary>
        public object SceneObject { get; }

        public GenerationContext(string sceneName, int floorNumber, GenerationPhase currentPhase, IModContainer owner, object sceneObject)
        {
            SceneName = sceneName;
            FloorNumber = floorNumber;
            CurrentPhase = currentPhase;
            Owner = owner;
            SceneObject = sceneObject;
        }
    }
}
