using Microsoft.CodeAnalysis;

namespace Baldic.Analyzers
{
    internal static class BaldicDiagnostics
    {
        private const string Category = "Baldic";

        // ── BALDIC002 ─────────────────────────────────────────────────────────
        public static readonly DiagnosticDescriptor EntrypointNotPublic = new DiagnosticDescriptor(
            id:                 "BALDIC002",
            title:              "Baldic entrypoint class must be public",
            messageFormat:      "Class '{0}' implements '{1}' but is not public. Baldic instantiates entrypoints via reflection; a non-public class will throw at runtime.",
            category:           Category,
            defaultSeverity:    DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:        "Baldic uses Activator.CreateInstance to instantiate entrypoint classes. " +
                                "The class must be public and have a public no-argument constructor.");

        // ── BALDIC005 ─────────────────────────────────────────────────────────
        public static readonly DiagnosticDescriptor PatchConditionWithoutHarmonyPatch = new DiagnosticDescriptor(
            id:                 "BALDIC005",
            title:              "[BaldicPatchCondition] on a class without [HarmonyPatch]",
            messageFormat:      "Class '{0}' has [BaldicPatchCondition] but no [HarmonyPatch] attribute. The condition will never be evaluated.",
            category:           Category,
            defaultSeverity:    DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:        "BaldicPatchConditionAttribute is only meaningful on Harmony patch classes. " +
                                "Add [HarmonyPatch(...)] to the class, or remove [BaldicPatchCondition].");
    }
}
