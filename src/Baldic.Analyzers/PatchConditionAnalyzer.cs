using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Baldic.Analyzers
{
    /// <summary>
    /// BALDIC005 — Flags classes with <c>[BaldicPatchCondition]</c> that have no <c>[HarmonyPatch]</c>.
    /// Without HarmonyPatch the condition is never evaluated by the patching pipeline.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PatchConditionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(BaldicDiagnostics.PatchConditionWithoutHarmonyPatch);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
        {
            var type = (INamedTypeSymbol)ctx.Symbol;
            if (type.TypeKind != TypeKind.Class) return;

            bool hasBaldicCondition = type.GetAttributes()
                .Any(a => a.AttributeClass?.Name is
                    "BaldicPatchConditionAttribute" or "BaldicPatchCondition");

            if (!hasBaldicCondition) return;

            bool hasHarmonyPatch = type.GetAttributes()
                .Any(a =>
                {
                    string? name = a.AttributeClass?.Name;
                    return name is "HarmonyPatch" or "HarmonyPatchAttribute"
                               or "HarmonyPatchAll" or "HarmonyPatchAllAttribute";
                });

            if (!hasHarmonyPatch)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    BaldicDiagnostics.PatchConditionWithoutHarmonyPatch,
                    type.Locations[0],
                    type.Name));
            }
        }
    }
}
