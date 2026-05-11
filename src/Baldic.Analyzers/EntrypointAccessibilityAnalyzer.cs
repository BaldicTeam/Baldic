using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Baldic.Analyzers
{
    /// <summary>
    /// BALDIC002 — Flags Baldic entrypoint classes that are not <c>public</c>.
    /// Baldic uses <c>Activator.CreateInstance</c> at runtime; non-public classes throw.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EntrypointAccessibilityAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> EntrypointInterfaces =
            ImmutableHashSet.Create(
                "IBaldicModInitializer",
                "IBaldicAssetsLoadedEntrypoint",
                "IBaldicGeneratorEntrypoint",
                "IBaldicPreLaunchEntrypoint",
                "IBaldicOptionsEntrypoint");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(BaldicDiagnostics.EntrypointNotPublic);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
        {
            var type = (INamedTypeSymbol)ctx.Symbol;
            if (type.TypeKind != TypeKind.Class || type.IsAbstract) return;
            if (type.DeclaredAccessibility == Accessibility.Public) return;

            foreach (var iface in type.AllInterfaces)
            {
                if (!EntrypointInterfaces.Contains(iface.Name)) continue;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    BaldicDiagnostics.EntrypointNotPublic,
                    type.Locations[0],
                    type.Name,
                    iface.Name));
                return;
            }
        }
    }
}
