using System;
using System.Collections.Generic;
using Baldic.Loader.Abstractions;

namespace Baldic.API.UI.Options
{
    /// <summary>
    /// Registry for mod options menu categories.
    /// Mods register categories during the <c>MainInitialize</c> lifecycle stage.
    /// The options menu MonoBehaviour reads this registry to build the menu.
    ///
    /// Safe to call multiple times — menu may be rebuilt when settings are reopened.
    /// </summary>
    public sealed class OptionsRegistry
    {
        public static readonly OptionsRegistry Instance = new OptionsRegistry();

        private readonly object _lock = new object();
        private readonly List<OptionsCategory> _categories = new List<OptionsCategory>();

        private OptionsRegistry() { }

        /// <summary>
        /// Register an options category for the given mod.
        /// Returns the category builder so controls can be added fluently.
        /// </summary>
        public OptionsCategory AddCategory(NamespacedId id, IModContainer owner, string displayName)
        {
            lock (_lock)
            {
                if (_categories.Exists(c => c.Id == id))
                    throw new ArgumentException($"Options category '{id}' is already registered.");

                var cat = new OptionsCategory(id, owner, displayName);
                _categories.Add(cat);
                return cat;
            }
        }

        public IReadOnlyList<OptionsCategory> GetAll()
        {
            lock (_lock) { return new List<OptionsCategory>(_categories); }
        }

        public IReadOnlyList<OptionsCategory> GetByOwner(string modId)
        {
            lock (_lock)
            {
                return _categories.FindAll(c => c.Owner.Id == modId);
            }
        }
    }

    /// <summary>An options category owned by one mod.</summary>
    public sealed class OptionsCategory
    {
        public NamespacedId Id { get; }
        public IModContainer Owner { get; }
        public string DisplayName { get; }
        private readonly List<OptionsControl> _controls = new List<OptionsControl>();

        internal OptionsCategory(NamespacedId id, IModContainer owner, string displayName)
        {
            Id = id;
            Owner = owner;
            DisplayName = displayName;
        }

        public IReadOnlyList<OptionsControl> Controls => _controls;

        public OptionsCategory Toggle(NamespacedId id, string label, bool defaultValue,
            Action<bool>? onChange = null)
        {
            _controls.Add(new OptionsControl(id, OptionsControlKind.Toggle, label,
                new OptionsControlConfig { DefaultBool = defaultValue, OnBoolChanged = onChange }));
            return this;
        }

        public OptionsCategory Slider(NamespacedId id, string label, float min, float max, float defaultValue,
            Action<float>? onChange = null)
        {
            _controls.Add(new OptionsControl(id, OptionsControlKind.Slider, label,
                new OptionsControlConfig { Min = min, Max = max, DefaultFloat = defaultValue, OnFloatChanged = onChange }));
            return this;
        }

        public OptionsCategory Choice(NamespacedId id, string label, IReadOnlyList<string> choices,
            int defaultIndex = 0, Action<int>? onChange = null)
        {
            _controls.Add(new OptionsControl(id, OptionsControlKind.Choice, label,
                new OptionsControlConfig { Choices = choices, DefaultInt = defaultIndex, OnIntChanged = onChange }));
            return this;
        }
    }

    public enum OptionsControlKind { Toggle, Slider, Choice }

    public sealed class OptionsControl
    {
        public NamespacedId Id { get; }
        public OptionsControlKind Kind { get; }
        public string Label { get; }
        public OptionsControlConfig Config { get; }

        internal OptionsControl(NamespacedId id, OptionsControlKind kind, string label, OptionsControlConfig config)
        {
            Id = id; Kind = kind; Label = label; Config = config;
        }
    }

    public sealed class OptionsControlConfig
    {
        public bool DefaultBool { get; set; }
        public float DefaultFloat { get; set; }
        public float Min { get; set; }
        public float Max { get; set; } = 1f;
        public int DefaultInt { get; set; }
        public IReadOnlyList<string>? Choices { get; set; }
        public Action<bool>? OnBoolChanged { get; set; }
        public Action<float>? OnFloatChanged { get; set; }
        public Action<int>? OnIntChanged { get; set; }
    }
}
