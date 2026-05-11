using System;
using System.Collections.Generic;
using Baldic.Loader.Abstractions.Entrypoints;

namespace Baldic.API.UI.LoadingScreen
{
    /// <summary>
    /// Concrete <see cref="IProgressReporter"/> that stores progress state
    /// and fires events the loading screen can subscribe to.
    ///
    /// The loading screen MonoBehaviour should subscribe to <see cref="OnStep"/>
    /// and <see cref="OnWarning"/> and update the UI accordingly.
    /// </summary>
    public sealed class ProgressReporter : IProgressReporter
    {
        private int _total;
        private int _current;
        private readonly List<string> _warnings = new List<string>();

        public event Action<int>? TotalChanged;
        public event Action<string>? OnStep;
        public event Action<string>? OnWarning;

        public int Total => _total;
        public int Current => _current;
        public IReadOnlyList<string> Warnings => _warnings;

        public void SetTotal(int total)
        {
            _total = Math.Max(1, total);
            _current = 0;
            TotalChanged?.Invoke(_total);
        }

        public void Step(string message)
        {
            _current++;
            OnStep?.Invoke(message);
        }

        public void Warning(string message)
        {
            _warnings.Add(message);
            OnWarning?.Invoke(message);
        }

        public float Progress => _total > 0 ? (float)_current / _total : 0f;

        public void Reset()
        {
            _total = 0;
            _current = 0;
            _warnings.Clear();
        }
    }

    /// <summary>
    /// No-op reporter used when no loading screen is active.
    /// </summary>
    public sealed class NullProgressReporter : IProgressReporter
    {
        public static readonly IProgressReporter Instance = new NullProgressReporter();
        public void SetTotal(int total) { }
        public void Step(string message) { }
        public void Warning(string message) { }
    }
}
