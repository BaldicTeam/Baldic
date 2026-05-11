using System;
using Baldic.Loader.Abstractions.Entrypoints;

namespace MyMod
{
    /// <summary>
    /// Main entrypoint for MyMod.
    /// Called during Baldic's <c>MainInitialize</c> lifecycle stage.
    ///
    /// Reference: <c>baldic.mod.json</c> → <c>entrypoints.main</c>
    /// </summary>
    public sealed class MyModInitializer : IBaldicModInitializer
    {
        public void OnInitialize(ModInitializationContext context)
        {
            Console.WriteLine($"[{context.Mod.Id}] Hello from {context.Mod.Id} v{context.Mod.Version}!");

            // TODO: register harmony patches, save handlers, options categories, etc.
        }
    }
}
