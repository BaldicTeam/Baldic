using System;

namespace Baldic.API.Resources.Assets
{
    /// <summary>
    /// A loaded AssetBundle owned by a mod. Explicit lifetime management —
    /// call <see cref="IDisposable.Dispose"/> when no longer needed,
    /// or keep alive for the entire session via <c>MarkPermanent()</c>.
    ///
    /// NOTE: UnityEngine types (AssetBundle, Object) are referenced symbolically
    /// here to allow this project to compile without Unity assemblies present.
    /// The implementation in <see cref="ModAssetBundle"/> uses reflection to
    /// call Unity APIs, resolved at runtime.
    /// </summary>
    public interface IModAssetBundle : IDisposable
    {
        /// <summary>Name of the underlying AssetBundle.</summary>
        string BundleName { get; }

        bool IsLoaded { get; }

        /// <summary>
        /// Load a named asset from the bundle.
        /// Returns null if the asset is not found.
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object subtype.</typeparam>
        T? LoadAsset<T>(string name) where T : class;

        /// <summary>
        /// Keep this bundle loaded for the entire game session.
        /// Calling Dispose after this is a no-op.
        /// </summary>
        void MarkPermanent();
    }
}
