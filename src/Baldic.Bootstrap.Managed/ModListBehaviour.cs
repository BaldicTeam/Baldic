using System.Collections.Generic;
using System.Text;
using Baldic.Loader.Abstractions;
using UnityEngine;

namespace Baldic.Bootstrap.Managed
{
    /// <summary>
    /// In-game mod list overlay.
    /// Toggle with the configured key (default: F1).
    /// Persists across scene loads via <c>DontDestroyOnLoad</c>.
    /// Added to the scene by <see cref="UnityHookRunner"/> after first scene load.
    /// </summary>
    internal sealed class ModListBehaviour : MonoBehaviour
    {
        private bool _visible;
        private string _content = string.Empty;
        private Vector2 _scroll;

        private static readonly KeyCode ToggleKey = KeyCode.F1;

        private const float PanelWidth  = 340f;
        private const float PanelHeight = 420f;

        internal void Init(IReadOnlyList<IModContainer> mods)
        {
            DontDestroyOnLoad(gameObject);

            var sb = new StringBuilder();
            sb.AppendLine($"Baldic — {mods.Count} mod(s) loaded");
            sb.AppendLine(new string('─', 38));
            foreach (var mod in mods)
            {
                if (mod.IsBuiltIn) continue;
                sb.AppendLine($"  {mod.Id}  v{mod.Version}");
                if (!string.IsNullOrEmpty(mod.Manifest?.Name))
                    sb.AppendLine($"    {mod.Manifest.Name}");
            }
            _content = sb.ToString();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            float x = 10f;
            float y = (Screen.height - PanelHeight) / 2f;

            GUI.Box(new Rect(x, y, PanelWidth, PanelHeight), string.Empty);

            GUILayout.BeginArea(new Rect(x + 8, y + 8, PanelWidth - 16, PanelHeight - 16));
            GUILayout.Label($"<b>Baldic Mod List</b>  [F1 to close]");
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(_content);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }
    }
}
