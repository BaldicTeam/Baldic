using System.Collections.Generic;
using Mono.Cecil;

namespace Baldic.Patching.Cecil
{
    /// <summary>
    /// Pre-load IL patcher contract.
    /// Implement this in a dedicated assembly declared in <c>patches.cecil</c>.
    /// Cecil patchers run before the target assembly is loaded into the AppDomain.
    /// Output is cached; the target game file is never modified.
    /// </summary>
    public interface IBaldicCecilPatcher
    {
        /// <summary>
        /// File names of assemblies this patcher targets, e.g. "Assembly-CSharp.dll".
        /// </summary>
        IEnumerable<string> TargetAssemblies { get; }

        /// <summary>
        /// Apply IL transformations to the given <paramref name="assembly"/>.
        /// This method is called once per target assembly per game launch (unless cached).
        /// Do not save the assembly — the pipeline handles writing to cache.
        /// </summary>
        void Patch(CecilPatchContext context, AssemblyDefinition assembly);
    }

    /// <summary>
    /// Context passed to <see cref="IBaldicCecilPatcher.Patch"/>.
    /// </summary>
    public sealed class CecilPatchContext
    {
        /// <summary>The mod container that owns this patcher.</summary>
        public Loader.Abstractions.IModContainer Mod { get; }

        /// <summary>Baldic loader facade.</summary>
        public Loader.Abstractions.IBaldicLoader Loader { get; }

        /// <summary>Logger for the patching phase.</summary>
        public Loader.Abstractions.IBaldicLog Log { get; }

        /// <summary>Path to the original (unpatched) assembly on disk.</summary>
        public string OriginalAssemblyPath { get; }

        public CecilPatchContext(
            Loader.Abstractions.IModContainer mod,
            Loader.Abstractions.IBaldicLoader loader,
            Loader.Abstractions.IBaldicLog log,
            string originalAssemblyPath)
        {
            Mod = mod;
            Loader = loader;
            Log = log;
            OriginalAssemblyPath = originalAssemblyPath;
        }
    }
}
