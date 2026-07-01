// SPDX-FileCopyrightText: 2026 belshftl
// SPDX-License-Identifier: LGPL-3.0-only WITH AdditionRef-LGPLv3-Celeste-Target-Platform-Exception

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Reprimand.Analyzers.Core;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LifecycleAttrMethodAnalyzer : DiagnosticAnalyzer {
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		Diagnostics.Core.InvalidLifecycleAttrMethodCandidate,
		Diagnostics.Core.InvalidUnconditionalLifecycleAttrMethodParams,
		Diagnostics.Core.InvalidDepConditionalLifecycleAttrMethodParams
	);

	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static ctx => {
				KnownTypes known = new(ctx.Compilation);
				if (
					known.OnLoadAttribute is null ||
					known.OnUnloadAttribute is null ||
					known.OnLoadWithOptionalDepAttribute is null ||
					known.OnUnloadWithOptionalDepAttribute is null ||
					known.EverestModule is null
				)
					return;
				ctx.RegisterSymbolAction(c => analyzeMethod(c, known), SymbolKind.Method);
			}
		);
	}

	private static void analyzeMethod(SymbolAnalysisContext ctx, KnownTypes known) {
		var sym = (IMethodSymbol)ctx.Symbol;
		AttributeData? attr = null;
		bool isDepConditional = false;
		foreach (AttributeData a in sym.GetAttributes()) {
			if (
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadAttribute) ||
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnUnloadAttribute)
			) {
				attr = a;
			} else if (
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnLoadWithOptionalDepAttribute) ||
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.OnUnloadWithOptionalDepAttribute)
			) {
				attr = a;
				isDepConditional = true;
			}
		}
		if (attr is null)
			return;
		Location loc = attr.ApplicationSyntaxReference?.GetSyntax(ctx.CancellationToken).GetLocation() ??
			sym.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
		if (!sym.IsStatic || sym.IsGenericMethod)
			ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.InvalidLifecycleAttrMethodCandidate, loc, sym.Name));

		if (!isDepConditional) {
			if (sym.Parameters.Length != 0)
				ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.InvalidUnconditionalLifecycleAttrMethodParams, loc, sym.Name));
		} else {
			if (sym.Parameters.Length != 0 &&
				!(
					sym.Parameters.Length == 1 && sym.Parameters[0].RefKind == RefKind.None &&
					SymbolEqualityComparer.Default.Equals(sym.Parameters[0].Type, known.EverestModule)
				)
			)
				ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.Core.InvalidDepConditionalLifecycleAttrMethodParams, loc, sym.Name));
		}
	}
}
